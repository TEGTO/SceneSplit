import { CommonModule } from '@angular/common';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatButtonModule } from '@angular/material/button';
import { MAT_SNACK_BAR_DATA, MatSnackBarModule, MatSnackBarRef } from '@angular/material/snack-bar';
import { By } from '@angular/platform-browser';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ErrorAnnotatedComponent } from './error-annotated.component';

describe('ErrorAnnotatedComponent', () => {
  let fixture: ComponentFixture<ErrorAnnotatedComponent>;
  let mockSnackBarRef: MatSnackBarRef<ErrorAnnotatedComponent>;

  beforeEach(async () => {
    mockSnackBarRef = jasmine.createSpyObj('MatSnackBarRef', ['dismissWithAction']);

    await TestBed.configureTestingModule({
      imports: [
        MatSnackBarModule,
        NoopAnimationsModule,
        CommonModule,
        MatButtonModule,
        ErrorAnnotatedComponent
      ],
      providers: [
        { provide: MAT_SNACK_BAR_DATA, useValue: { messages: ['Error 1', 'Error 2'] } },
        { provide: MatSnackBarRef, useValue: mockSnackBarRef }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ErrorAnnotatedComponent);
    fixture.detectChanges();
  });

  it('should display error messages', () => {
    const messages = fixture.debugElement.queryAll(By.css('.text-base'));
    expect(messages.length).toBe(2);
    expect(messages[0].nativeElement.textContent).toContain('❌: Error 1');
    expect(messages[1].nativeElement.textContent).toContain('❌: Error 2');
  });

  it('should call dismissWithAction on dismiss button click', () => {
    const button = fixture.debugElement.query(By.css('button'));
    button.nativeElement.click();
    expect(mockSnackBarRef.dismissWithAction).toHaveBeenCalled();
  });
});
