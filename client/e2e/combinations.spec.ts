import { expect, test, type Page } from '@playwright/test';

async function start(page: Page, n: number): Promise<void> {
  await page.goto('/');
  await page.getByLabel('כמה מספרים לסדר?').fill(String(n));
  await page.getByRole('button', { name: 'התחלה' }).click();
  await expect(page.locator('.summary')).toBeVisible();
}

test('shows the first two n=3 permutations in exact lexicographic order', async ({ page }) => {
  await start(page, 3);

  const card = page.locator('.permutation-card');
  await page.getByRole('button', { name: 'הבא' }).click();
  await expect(card).toHaveText('קומבינציה מספר 1 – [1,2,3]');

  await page.getByRole('button', { name: 'הבא' }).click();
  await expect(card).toHaveText('קומבינציה מספר 2 – [1,3,2]');
});

test('shows the only n=1 permutation and disables Next', async ({ page }) => {
  await start(page, 1);
  await page.getByRole('button', { name: 'הבא' }).click();

  await expect(page.locator('.permutation-card')).toHaveText('קומבינציה מספר 1 – [1]');
  await expect(page.locator('.notice')).toHaveText('אין יותר קומבינציות');
  await expect(page.getByRole('button', { name: 'הבא' })).toBeDisabled();
});

test('shows the n=20 total and can jump to the last all-view page', async ({ page }) => {
  await start(page, 20);
  await expect(page.locator('.summary strong')).toHaveText('2,432,902,008,176,640,000');

  await page.getByRole('button', { name: 'כל הקומבינציות' }).click();
  await expect(page.locator('.pager-options select')).toHaveValue('10');
  await expect(page.locator('.permutation-list li')).toHaveCount(10);
  const lastPage = '243290200817664000';
  await page.locator('#go-to-page').fill(lastPage);
  await page.getByRole('button', { name: 'עבור' }).click();

  await expect(page.locator('.page-summary')).toHaveText(
    'עמוד 243,290,200,817,664,000 מתוך 243,290,200,817,664,000'
  );
  await expect(page.locator('.permutation-list li').last()).toHaveText(
    'קומבינציה מספר 2,432,902,008,176,640,000[20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1]'
  );
});

test('continues after returning from all-view at its last item', async ({ page }) => {
  await start(page, 20);
  const nextButton = page.getByRole('button', { name: 'הבא' });

  await nextButton.click();
  await expect(page.locator('.permutation-card .index')).toHaveText('קומבינציה מספר 1 –');
  await nextButton.click();
  await expect(page.locator('.permutation-card .index')).toHaveText('קומבינציה מספר 2 –');

  await page.getByRole('button', { name: 'כל הקומבינציות' }).click();
  const items = page.locator('.permutation-list li');
  await expect(items.first().locator('span')).toHaveText('קומבינציה מספר 3');
  await expect(items.last().locator('span')).toHaveText('קומבינציה מספר 22');

  await page.getByRole('button', { name: 'חזור' }).click();
  await expect(page.locator('.permutation-card .index')).toHaveText('קומבינציה מספר 22 –');
  await page.getByRole('button', { name: 'הבא' }).click();
  await expect(page.locator('.permutation-card')).toHaveText(
    'קומבינציה מספר 23 – [1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,20,19,17,18]'
  );
});
