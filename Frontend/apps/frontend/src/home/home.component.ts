import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TryDurableFunctionComponent } from '../try-durable-function/try-durable-function.component';
import { TryFunctionComponent } from '../try-function/try-function.component';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Subscription, tap } from 'rxjs';

@Component({
  selector: 'app-home',
  imports: [CommonModule, ReactiveFormsModule, TryFunctionComponent, TryDurableFunctionComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent implements OnInit, OnDestroy {
  readonly base ="https://ya5func13ackground.azurewebsites.net"
  // readonly base ="http://localhost:7071"
  readonly url1Base = `${this.base}/api/HttpToServiceBus`;
  readonly url2Base = `${this.base}/api/sampledurablefunction_httpstart`;

  url1 = '';
  url2 = '';

  formBuilder = inject(FormBuilder);
  form1 = this.formBuilder.group({
    name: this.formBuilder.control(
      { value: '', disabled: false },
      {
        nonNullable: true,
        validators: [ Validators.required ],
      }
    ),
  });
  form2 = this.formBuilder.group({
    name: this.formBuilder.control(
      { value: '', disabled: false },
      {
        nonNullable: true,
        validators: [ Validators.required ],
      }
    ),
  });

  subscription1?: Subscription;
  subscription2?: Subscription;

  ngOnInit(): void {
    this.subscription1 = this.form1.valueChanges
      .pipe(
        tap(value => {
          this.url1 = `${this.url1Base}?name=${value.name}`;
        }),
      )
      .subscribe();
    this.form1.setValue({
      name: 'Yas',
    });

    this.subscription2 = this.form2.valueChanges
      .pipe(
        tap(value => {
          this.url2 = `${this.url2Base}?name=${value.name}`;
        }),
      )
      .subscribe();
    this.form2.setValue({
      name: 'Joe',
    });
  }

  ngOnDestroy(): void {
    this.subscription1?.unsubscribe();
    this.subscription2?.unsubscribe();
  }
}
