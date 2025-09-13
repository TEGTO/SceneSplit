/* eslint-disable @typescript-eslint/no-explicit-any */
import { Injectable } from '@angular/core';
import { AbstractControl } from '@angular/forms';

@Injectable({
  providedIn: 'root'
})
export class ValidationMessage implements ValidationMessage {
  private readonly errorMessages: Record<string, string> = {
    required: "Field is required.",
    email: "Email must be in the form: example@mail.com.",
    invalidPhone: "Phone number must be the form: 0123456789.",
    minlength: "Input must have more characters.",
    maxlength: "Input must have less characters.",
    nonAlphanumeric: "Input must have non-alphanumeric characters.",
    digit: "Input must have digits.",
    uppercase: "Input must have upper case characters.",
    passwordNoMatch: "Passwords don't match.",
    invalidMinDate: "Date must be greater.",
    dateInPast: "Date must be in the past.",
    min: "Field must be bigger.",
    max: "Field must be smaller.",
    notEmptyString: "Field must not be an empty string.",
    dateRangeFromInvalid: '"From" date must be before or equal to "To" date.',
    dateRangeToInvalid: '"To" date must be after or equal to "From" date.',
    rangeMinInvalid: '"Min" must be less than or equal to "Max".',
    rangeMaxInvalid: '"Max" must be greater than or equal to "Min".',
    noSpaces: 'Input must not contain spaces.',
    invalidSelectInput: 'Item must be selected.'
  };

  getValidationMessage(input: AbstractControl<any, any>): { hasError: boolean; message: string } {
    const errorKey = Object.keys(this.errorMessages).find(key => input.hasError(key));
    return errorKey
      ? { hasError: true, message: this.errorMessages[errorKey] }
      : { hasError: false, message: "" };
  }
}
