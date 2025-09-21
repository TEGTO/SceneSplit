import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Store } from '@ngrx/store';
import { sendSceneImageFile } from '../..';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-book-table',
  templateUrl: './scene-split-image-drop-down.component.html',
  styleUrl: './scene-split-image-drop-down.component.scss'
})
export class SceneSplitImageDropDownComponent implements OnInit {
  @ViewChild('fileInputRef') fileInputRef!: ElementRef;
  formGroup: FormGroup = null!;
  fileError: string | null = null;

  get fileInput() { return this.formGroup.get('file') as FormControl; }
  get fileSizeStr() { return `${environment.maxFileSize / (1024 * 1024)}MB`; }

  constructor(private readonly store: Store) { }

  ngOnInit(): void {
    this.formGroup = new FormGroup({
      file: new FormControl(null, [Validators.required])
    });
  }

  onFileSelected(event: Event) {
    const inputElement = event.target as HTMLInputElement;
    if (inputElement.files && inputElement.files.length > 0) {

      const file = inputElement.files[0];

      if (file.size > environment.maxFileSize) {
        this.fileError = `File size must be ${this.fileSizeStr} or less.`;
        this.fileInput.setValue(null);
        return;
      }

      if (!environment.allowedImageTypes.includes(file.type)) {
        this.fileError = 'Only PNG and JPG files are allowed.';
        this.fileInput.setValue(null);
        return;
      }

      this.fileError = null;

      this.fileInput.setValue(file);
      this.fileInput.markAsTouched();
    }

    this.sendSceneImageFile();
  }

  sendSceneImageFile() {
    if (this.formGroup.valid) {
      this.store.dispatch(sendSceneImageFile({ file: this.fileInput.value }));
    }
  }
}