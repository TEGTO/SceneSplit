import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { RouterModule, Routes } from '@angular/router';
import { EffectsModule } from '@ngrx/effects';
import { StoreModule } from '@ngrx/store';
import { BookEffects, SceneSplitImageDropDownComponent, sceneSplitReducer } from '.';
import { SceneSplitObjectImageTableComponent } from './components/scene-split-object-image-table/scene-split-object-image-table.component';
import { SceneSplitViewComponent } from './components/scene-split-view/scene-split-view.component';

const routes: Routes = [
  {
    path: "", component: SceneSplitViewComponent,
  },
];

@NgModule({
  declarations: [
    SceneSplitImageDropDownComponent,
    SceneSplitViewComponent,
    SceneSplitObjectImageTableComponent,
  ],
  imports: [
    CommonModule,
    MatDialogModule,
    RouterModule.forChild(routes),
    MatInputModule,
    FormsModule,
    MatFormFieldModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatTableModule,
    StoreModule.forFeature('scene-split', sceneSplitReducer),
    EffectsModule.forFeature([BookEffects]),
  ]
})
export class SceneSplitModule { }
