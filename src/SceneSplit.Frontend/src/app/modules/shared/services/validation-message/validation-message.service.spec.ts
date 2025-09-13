import { TestBed } from '@angular/core/testing';
import { AbstractControl, FormControl, ReactiveFormsModule } from '@angular/forms';
import { ValidationMessage } from './validation-message.service';

describe('ValidationMessageService', () => {
  let service: ValidationMessage;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ReactiveFormsModule],
      providers: [ValidationMessage]
    });
    service = TestBed.inject(ValidationMessage);
  });

  const testCases: { errorKey: string, expectedMessage: string }[] = [
    { errorKey: 'required', expectedMessage: "Field is required." },
    { errorKey: 'email', expectedMessage: "Email must be in the form: example@mail.com." },
    { errorKey: 'minlength', expectedMessage: "Input must have more characters." },
    { errorKey: 'maxlength', expectedMessage: "Input must have less characters." },
    { errorKey: 'nonAlphanumeric', expectedMessage: "Input must have non-alphanumeric characters." },
    { errorKey: 'invalidPhone', expectedMessage: "Phone number must be the form: 0123456789." },
    { errorKey: 'digit', expectedMessage: "Input must have digits." },
    { errorKey: 'uppercase', expectedMessage: "Input must have upper case characters." },
    { errorKey: 'passwordNoMatch', expectedMessage: "Passwords don't match." },
    { errorKey: 'invalidMinDate', expectedMessage: "Date must be greater." },
    { errorKey: 'dateInPast', expectedMessage: "Date must be in the past." },
    { errorKey: 'min', expectedMessage: "Field must be bigger." },
    { errorKey: 'max', expectedMessage: "Field must be smaller." },
    { errorKey: 'dateRangeFromInvalid', expectedMessage: '"From" date must be before or equal to "To" date.' },
    { errorKey: 'dateRangeToInvalid', expectedMessage: '"To" date must be after or equal to "From" date.' },
    { errorKey: 'rangeMinInvalid', expectedMessage: '"Min" must be less than or equal to "Max".' },
    { errorKey: 'rangeMaxInvalid', expectedMessage: '"Max" must be greater than or equal to "Min".' },
    { errorKey: 'noSpaces', expectedMessage: 'Input must not contain spaces.' },
    { errorKey: 'invalidSelectInput', expectedMessage: 'Item must be selected.' }
  ];

  testCases.forEach(({ errorKey, expectedMessage }) => {
    it(`getValidationMessage with ${errorKey} error returns correct message`, () => {
      const control = new FormControl();
      control.setErrors({ [errorKey]: true });

      const result = service.getValidationMessage(control as AbstractControl);
      expect(result).toEqual({ hasError: true, message: expectedMessage });
    });
  });

  it('getValidationMessage with no errors returns empty message', () => {
    const control = new FormControl();
    const result = service.getValidationMessage(control as AbstractControl);
    expect(result).toEqual({ hasError: false, message: "" });
  });
});
