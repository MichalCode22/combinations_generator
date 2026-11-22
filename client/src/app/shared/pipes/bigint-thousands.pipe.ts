import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'bigintThousands',
  standalone: true
})
export class BigintThousandsPipe implements PipeTransform {
  private readonly formatter = new Intl.NumberFormat('en-US');

  transform(value: bigint | null | undefined): string {
    return value === null || value === undefined ? '' : this.formatter.format(value);
  }
}
