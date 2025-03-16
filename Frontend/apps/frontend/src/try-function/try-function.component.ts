import { Component, ContentChild, ContentChildren, inject, Input, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription, tap } from 'rxjs';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-try-function',
  imports: [CommonModule],
  templateUrl: './try-function.component.html',
  styleUrls: ['../try-function-common.scss', './try-function.component.scss'],
})
export class TryFunctionComponent implements OnDestroy {
  @Input() title: string = '';
  @Input() url?: string;

  http = inject(HttpClient);

  subscription: Subscription|null = null;
  subscription2: Subscription|null = null;
  isLoading: boolean = false;
  result: string|null = '';

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
    this.subscription2?.unsubscribe();
  }
  
  try(): void {
    if (this.url == null) {
      throw 'The URL to an Azure Function is not configured!';
    }
    this.isLoading = true;
    this.subscription = this.http
      .get<Record<string, string>>(this.url)
      .pipe(
        tap(res => {
          this.isLoading = false;
          this.result = JSON.stringify(res, null, 2);
        }),
      )
      .subscribe();
  }
}
