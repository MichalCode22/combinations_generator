using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using CombinationsGenerator.Application.Services;
using CombinationsGenerator.Application.SessionStore;
using CombinationsGenerator.Domain;
using CombinationsGenerator.Infrastructure.SessionStore;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace CombinationsGenerator.Application.Tests;

public sealed class PermutationServicesTests
{
    [Fact]
    public async Task Next_returns_lexicographic_sequence_and_remains_unchanged_after_exhaustion()
    {
        var fixture = CreateFixture();
        var start = await fixture.Start.StartAsync(new StartRequest(3));
        var shown = new List<int[]>();

        for (var index = 1; index <= 6; index++)
        {
            var next = await fixture.Next.NextAsync(start.SessionId);
            next.HasMore.Should().BeTrue();
            next.Index.Should().Be(index.ToString(CultureInfo.InvariantCulture));
            shown.Add(next.Permutation!);
        }

        shown.Should().BeEquivalentTo(
            [
                new[] { 1, 2, 3 },
                new[] { 1, 3, 2 },
                new[] { 2, 1, 3 },
                new[] { 2, 3, 1 },
                new[] { 3, 1, 2 },
                new[] { 3, 2, 1 }
            ],
            options => options.WithStrictOrdering());

        var current = await fixture.Current.GetCurrentAsync(start.SessionId);
        var exhausted = await fixture.Next.NextAsync(start.SessionId);
        var repeatedExhausted = await fixture.Next.NextAsync(start.SessionId);

        exhausted.HasMore.Should().BeFalse();
        exhausted.Message.Should().Be("אין יותר קומבינציות");
        repeatedExhausted.Should().Be(exhausted);
        (await fixture.Current.GetCurrentAsync(start.SessionId)).Should().BeEquivalentTo(current);
    }

    [Fact]
    public async Task All_supports_single_default_and_maximum_page_sizes()
    {
        var fixture = CreateFixture();
        var start = await fixture.Start.StartAsync(new StartRequest(5));

        var single = await fixture.All.GetAllAsync(start.SessionId, pageSize: 1, enter: true);
        var defaultSize = await fixture.All.GetAllAsync(start.SessionId);
        var maximum = await fixture.All.GetAllAsync(start.SessionId, pageSize: 100);

        single.Items.Should().HaveCount(1);
        single.TotalPages.Should().Be("120");
        defaultSize.Items.Should().HaveCount(10);
        defaultSize.TotalPages.Should().Be("12");
        maximum.Items.Should().HaveCount(100);
        maximum.TotalPages.Should().Be("2");
    }

    [Fact]
    public async Task All_starts_after_last_shown_and_syncs_next_and_current()
    {
        var fixture = CreateFixture();
        var start = await fixture.Start.StartAsync(new StartRequest(4));
        await fixture.Next.NextAsync(start.SessionId);
        await fixture.Next.NextAsync(start.SessionId);
        await fixture.Next.NextAsync(start.SessionId);

        var page = await fixture.All.GetAllAsync(start.SessionId, pageSize: 2, enter: true);
        page.StartIndex.Should().Be("4");
        page.Items.Select(item => item.Index).Should().Equal("4", "5");

        (await fixture.Current.GetCurrentAsync(start.SessionId)).Index.Should().Be("5");
        var next = await fixture.Next.NextAsync(start.SessionId);
        next.Index.Should().Be("6");
        (await fixture.Current.GetCurrentAsync(start.SessionId)).Permutation.Should().BeEquivalentTo(
            fixture.Permutations.Unrank(5, 4));
    }

    [Fact]
    public async Task Entering_all_view_again_starts_after_the_last_shown_permutation()
    {
        var fixture = CreateFixture();
        var start = await fixture.Start.StartAsync(new StartRequest(5));
        await fixture.Next.NextAsync(start.SessionId);
        await fixture.Next.NextAsync(start.SessionId);

        var first = await fixture.All.GetAllAsync(start.SessionId, pageSize: 3, enter: true);
        first.Items.Select(item => item.Index).Should().Equal("3", "4", "5");

        (await fixture.Next.NextAsync(start.SessionId)).Index.Should().Be("6");
        var second = await fixture.All.GetAllAsync(start.SessionId, pageSize: 3, enter: true);

        second.StartIndex.Should().Be("7");
        second.Items.Select(item => item.Index).Should().Equal("7", "8", "9");
    }

    [Fact]
    public async Task All_last_page_is_partial_and_finishes_at_reverse_permutation_for_n_twenty()
    {
        var fixture = CreateFixture();
        var start = await fixture.Start.StartAsync(new StartRequest(20));
        var total = ulong.Parse(start.Total, CultureInfo.InvariantCulture);
        var pageSize = 100;
        var totalPages = (total - 1) / (ulong)pageSize + 1;

        var lastPage = await fixture.All.GetAllAsync(
            start.SessionId,
            pageSize,
            totalPages,
            enter: true);

        lastPage.Items.Should().HaveCount((int)(total - (totalPages - 1) * (ulong)pageSize));
        lastPage.Items[^1].Index.Should().Be(total.ToString(CultureInfo.InvariantCulture));
        lastPage.Items[^1].Permutation.Should().Equal(Enumerable.Range(1, 20).Reverse());
        lastPage.TotalPages.Should().Be(totalPages.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task All_total_pages_match_independent_big_integer_math()
    {
        var fixture = CreateFixture();
        var start = await fixture.Start.StartAsync(new StartRequest(20));
        await fixture.Next.NextAsync(start.SessionId);
        await fixture.Next.NextAsync(start.SessionId);
        const int size = 37;
        var response = await fixture.All.GetAllAsync(start.SessionId, size, enter: true);
        var total = BigInteger.Parse(start.Total, CultureInfo.InvariantCulture);
        var remaining = total - 2;
        var expectedPages = (remaining + size - 1) / size;

        response.TotalPages.Should().Be(expectedPages.ToString(CultureInfo.InvariantCulture));
        response.StartIndex.Should().Be("3");
    }

    [Fact]
    public async Task All_rejects_out_of_range_pages_and_page_sizes()
    {
        var fixture = CreateFixture();
        var start = await fixture.Start.StartAsync(new StartRequest(3));

        await FluentActions.Invoking(() => fixture.All.GetAllAsync(start.SessionId, page: 7))
            .Should().ThrowAsync<ApplicationValidationException>();
        await FluentActions.Invoking(() => fixture.All.GetAllAsync(start.SessionId, pageSize: 0))
            .Should().ThrowAsync<ApplicationValidationException>();
        await FluentActions.Invoking(() => fixture.All.GetAllAsync(start.SessionId, pageSize: 101))
            .Should().ThrowAsync<ApplicationValidationException>();
    }

    [Fact]
    public async Task Start_validates_n_and_supports_one_and_twenty()
    {
        var fixture = CreateFixture();
        await FluentActions.Invoking(() => fixture.Start.StartAsync(new StartRequest(0)))
            .Should().ThrowAsync<ApplicationValidationException>();
        await FluentActions.Invoking(() => fixture.Start.StartAsync(new StartRequest(21)))
            .Should().ThrowAsync<ApplicationValidationException>();

        var one = await fixture.Start.StartAsync(new StartRequest(1));
        (await fixture.Next.NextAsync(one.SessionId)).Permutation.Should().Equal(1);
        var twenty = await fixture.Start.StartAsync(new StartRequest(20));
        twenty.Total.Should().Be("2432902008176640000");
    }

    [Fact]
    public async Task All_returns_empty_when_no_permutations_remain()
    {
        var fixture = CreateFixture();
        var start = await fixture.Start.StartAsync(new StartRequest(1));
        await fixture.Next.NextAsync(start.SessionId);

        var response = await fixture.All.GetAllAsync(start.SessionId, enter: true);

        response.Items.Should().BeEmpty();
        response.TotalPages.Should().Be("0");
        response.Message.Should().Be("אין יותר קומבינציות");
    }

    [Fact]
    public async Task Reset_removes_session_and_a_new_start_can_use_another_n()
    {
        var fixture = CreateFixture();
        var first = await fixture.Start.StartAsync(new StartRequest(3));
        await fixture.Reset.ResetAsync(first.SessionId);
        fixture.Store.Count.Should().Be(0);
        await FluentActions.Invoking(() => fixture.Current.GetCurrentAsync(first.SessionId))
            .Should().ThrowAsync<SessionNotFoundException>();

        var second = await fixture.Start.StartAsync(new StartRequest(4));
        second.Total.Should().Be("24");
        (await fixture.Next.NextAsync(second.SessionId)).Permutation.Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public async Task Current_is_empty_before_first_permutation_is_shown()
    {
        var fixture = CreateFixture();
        var start = await fixture.Start.StartAsync(new StartRequest(3));

        var current = await fixture.Current.GetCurrentAsync(start.SessionId);

        current.Index.Should().BeNull();
        current.Permutation.Should().BeEmpty();
    }

    [Fact]
    public async Task Large_n_operations_return_only_requested_permutations_without_precomputation()
    {
        var fixture = CreateFixture();
        var stopwatch = Stopwatch.StartNew();
        var start = await fixture.Start.StartAsync(new StartRequest(20));
        for (var index = 0; index < 10; index++)
        {
            (await fixture.Next.NextAsync(start.SessionId)).Permutation.Should().HaveCount(20);
        }

        var page = await fixture.All.GetAllAsync(start.SessionId, pageSize: 100, enter: true);
        stopwatch.Stop();

        page.Items.Should().HaveCount(100);
        fixture.Store.Count.Should().Be(1);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Fifty_concurrent_sessions_advance_independently()
    {
        var fixture = CreateFixture();
        var starts = await Task.WhenAll(Enumerable.Range(1, 50)
            .Select(n => fixture.Start.StartAsync(new StartRequest(n % 10 + 1))));

        var results = await Task.WhenAll(starts.Select(start => fixture.Next.NextAsync(start.SessionId)));

        results.Should().HaveCount(50);
        results.Should().OnlyContain(result => result.Index == "1" && result.HasMore);
        results.Select(result => string.Join(',', result.Permutation!)).Distinct().Count().Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task One_hundred_parallel_next_calls_on_one_session_return_each_index_once()
    {
        var fixture = CreateFixture();
        var start = await fixture.Start.StartAsync(new StartRequest(7));

        var results = await Task.WhenAll(Enumerable.Range(0, 100)
            .Select(_ => fixture.Next.NextAsync(start.SessionId)));

        results.Select(result => ulong.Parse(result.Index!, CultureInfo.InvariantCulture))
            .Order()
            .Should()
            .Equal(Enumerable.Range(1, 100).Select(index => (ulong)index));
        results.Select(result => string.Join(',', result.Permutation!)).Distinct().Should().HaveCount(100);
    }

    [Fact]
    public async Task Store_slides_expiration_on_access_and_rejects_expired_or_unknown_sessions()
    {
        var timeProvider = new AdjustableTimeProvider();
        var store = CreateStore(slidingExpiration: TimeSpan.FromMinutes(10), timeProvider: timeProvider);
        var fixture = CreateFixture(store);
        var start = await fixture.Start.StartAsync(new StartRequest(2));
        timeProvider.Advance(TimeSpan.FromMinutes(9));
        await fixture.Current.GetCurrentAsync(start.SessionId);
        timeProvider.Advance(TimeSpan.FromMinutes(9));
        await fixture.Current.GetCurrentAsync(start.SessionId);
        timeProvider.Advance(TimeSpan.FromMinutes(11));

        await FluentActions.Invoking(() => fixture.Current.GetCurrentAsync(start.SessionId))
            .Should().ThrowAsync<SessionNotFoundException>();
        await FluentActions.Invoking(() => fixture.Current.GetCurrentAsync(Guid.NewGuid()))
            .Should().ThrowAsync<SessionNotFoundException>();
    }

    [Fact]
    public async Task Store_removes_sessions_and_enforces_maximum_session_count()
    {
        var store = CreateStore(maximumSessions: 2);
        var fixture = CreateFixture(store);
        var first = await fixture.Start.StartAsync(new StartRequest(1));
        var second = await fixture.Start.StartAsync(new StartRequest(1));

        await FluentActions.Invoking(() => fixture.Start.StartAsync(new StartRequest(1)))
            .Should().ThrowAsync<SessionLimitExceededException>();
        await fixture.Reset.ResetAsync(first.SessionId);
        var third = await fixture.Start.StartAsync(new StartRequest(1));
        store.Count.Should().Be(2);
        await fixture.Reset.ResetAsync(second.SessionId);
        await fixture.Reset.ResetAsync(third.SessionId);
        store.Count.Should().Be(0);
    }

    private static Fixture CreateFixture(InMemorySessionStore? store = null)
    {
        var actualStore = store ?? CreateStore();
        var permutations = new PermutationGenerator();
        return new Fixture(
            actualStore,
            permutations,
            new StartService(actualStore, permutations),
            new NextService(actualStore, permutations),
            new AllService(actualStore, permutations),
            new CurrentService(actualStore),
            new ResetService(actualStore));
    }

    private static InMemorySessionStore CreateStore(
        int maximumSessions = 10_000,
        TimeSpan? slidingExpiration = null,
        TimeProvider? timeProvider = null) =>
        new(
            Options.Create(new SessionStoreOptions
            {
                MaximumSessions = maximumSessions,
                SlidingExpiration = slidingExpiration ?? TimeSpan.FromMinutes(30)
            }),
            timeProvider);

    private sealed record Fixture(
        InMemorySessionStore Store,
        IPermutationGenerator Permutations,
        IStartService Start,
        INextService Next,
        IAllService All,
        ICurrentService Current,
        IResetService Reset);

    private sealed class AdjustableTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan amount) => _now += amount;
    }
}
