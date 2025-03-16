import { Component, ElementRef, inject, Input, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { mergeMap, Observable, of, Subscription, tap, timer } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-try-durable-function',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './try-durable-function.component.html',
  styleUrls: ['../try-function-common.scss', './try-durable-function.component.scss'],
})
export class TryDurableFunctionComponent implements OnInit, OnDestroy {
  @Input() title: string = '';
  @Input() url?: string;
  @Input() defaultSubQueryKey: string = 'statusQueryGetUri';

  http = inject(HttpClient);

  private formBuilder = inject(FormBuilder);
  form = this.formBuilder.group({
    subQueryKey: this.formBuilder.control('', { nonNullable: true }),
    isRepeated: this.formBuilder.control(false),
  });

  @ViewChild('apiOptionsSelect')
  apiOptionsSelect?: ElementRef<HTMLSelectElement>;

  formSubscription: Subscription|null = null;
  subscription: Subscription|null = null;
  subscription2: Subscription|null = null;
  isLoading: boolean = false;
  apiSpec: Record<string, string>|null = null;
  apiSpec4Display: string|null = '';
  subQueryKeys: string[] = [];
  subQueryKey?: string;
  result: string|null = '';

  ngOnInit(): void {
    this.formSubscription = this.form.valueChanges.pipe(
      tap(value => {
        console.log('form', value);
        this.try2();
      }),
    ).subscribe();
  }

  ngOnDestroy(): void {
    this.formSubscription?.unsubscribe();
    this.subscription?.unsubscribe();
    this.subscription2?.unsubscribe();
  }
  
  try(): void {
    if (this.url == null) {
      throw 'The URL to an Azure Function is not configured!';
    }
    this.isLoading = true;
    this.apiSpec = null;
    this.apiSpec4Display = null;
    this.subQueryKeys = [];
    this.subQueryKey = undefined;

    this.subscription?.unsubscribe();
    this.subscription2?.unsubscribe();

    this.subscription = this.http
      .get<Record<string, string>>(this.url)
      .pipe(
        tap(res => {
          this.isLoading = false;
          this.apiSpec = res;
          this.apiSpec4Display = JSON.stringify(res, null, 2);
          this.subQueryKeys = Object.keys(res);

          if (this.subQueryKeys.includes(this.defaultSubQueryKey)) {
            this.subQueryKey = this.defaultSubQueryKey;
            if (this.apiOptionsSelect) {

              this.form.setValue({
                subQueryKey: this.subQueryKey ?? '',
                isRepeated: true,
              });
            }
          }
        }),
      )
      .subscribe();
  }

  private try2(): void {
    this.subscription2?.unsubscribe();

    this.subscription2 = (
      this.form.value.isRepeated
      ? timer(0, 1000).pipe(
          mergeMap(() => this.innerTry2()),
        )
      : this.innerTry2())
      .subscribe();
  }

  private innerTry2(): Observable<void|object> {
    if (!this.apiSpec) {
      throw 'Start an orchestrator first!';
    }
    const key = this.form.value.subQueryKey ?? '';
    if (!Object.keys(this.apiSpec).includes(key)) {
      throw 'Select a valid sub query key.';
    }
    const url = this.apiSpec[key];
    if (!url) {
      console.error(`Couldn't find the sub query URL for '${key}'.`)
      return of();
    }
    
    this.isLoading = true;
    return this.http
      .get(url)
      .pipe(
        tap(res => {
          this.isLoading = false;
          this.result = JSON.stringify(res, null, 2);
        }),
      );
  }
}
