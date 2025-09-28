import { ChangeDetectorRef, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
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
  previewUrl: string | ArrayBuffer | null = null;
  isDragging = false;

  get fileInput() { return this.formGroup.get('file') as FormControl; }
  get fileSizeStr() { return `${environment.maxFileSize / (1024 * 1024)}MB`; }

  constructor(private readonly store: Store, private cdr: ChangeDetectorRef) { }

  ngOnInit(): void {
    this.formGroup = new FormGroup({
      file: new FormControl(null, [Validators.required])
    });
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent) {
    event.preventDefault();
    this.isDragging = false;
  }

  onFileDropped(event: DragEvent) {
    event.preventDefault();
    this.isDragging = false;

    if (event.dataTransfer && event.dataTransfer.files.length > 0) {
      const file = event.dataTransfer.files[0];

      this.handleFile(file);
    }
  }

  private handleFile(file: File) {
    if (file.size > environment.maxFileSize) {
      this.fileError = `File size must be ${this.fileSizeStr} or less.`;
      this.fileInput.setValue(null);
      this.previewUrl = null;
      return;
    }

    if (!environment.allowedImageTypes.includes(file.type)) {
      this.fileError = 'Only PNG and JPG files are allowed.';
      this.fileInput.setValue(null);
      this.previewUrl = null;
      return;
    }

    this.fileError = null;
    this.fileInput.setValue(file);
    this.fileInput.markAsTouched();

    const reader = new FileReader();
    reader.onload = () => {
      this.previewUrl = reader.result;
      this.cdr.detectChanges();
    };
    reader.readAsDataURL(file);

    this.sendSceneImageFile();
  }

  onFileSelected(event: Event) {
    const inputElement = event.target as HTMLInputElement;
    if (inputElement.files && inputElement.files.length > 0) {
      const file = inputElement.files[0];
      this.handleFile(file);
    } else {
      this.previewUrl = null;
    }
  }

  sendSceneImageFile() {
    if (this.formGroup.valid) {
      this.store.dispatch(sendSceneImageFile({ file: this.fileInput.value }));
    }
  }
}