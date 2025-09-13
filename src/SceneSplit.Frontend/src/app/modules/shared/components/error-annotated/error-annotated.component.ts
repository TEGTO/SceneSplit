/* eslint-disable @typescript-eslint/no-explicit-any */
import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_SNACK_BAR_DATA, MatSnackBarAction, MatSnackBarRef } from '@angular/material/snack-bar';

@Component({
  selector: 'app-error-annotated',
  standalone: true,
  imports: [MatButtonModule, MatSnackBarAction, CommonModule],
  templateUrl: './error-annotated.component.html',
  styleUrl: './error-annotated.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ErrorAnnotatedComponent {
  snackBarRef = inject(MatSnackBarRef);

  get messages() { return this.data.messages; }

  constructor(
    @Inject(MAT_SNACK_BAR_DATA) private readonly data: any
  ) { }

}
