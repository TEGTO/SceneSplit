import { CommonModule } from '@angular/common';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { RouterModule, Routes } from '@angular/router';
import { EffectsModule } from '@ngrx/effects';
import { StoreModule } from '@ngrx/store';
import { AppComponent, MainViewComponent } from '.';

const routes: Routes = [
  {
    path: "", component: MainViewComponent,
    children: [
      {
        path: "",
        loadChildren: () => import('../scene-split/scene-split.module').then(m => m.SceneSplitModule),
      },
    ]
  },
  { path: '**', redirectTo: '' }
];

@NgModule({
  declarations: [
    AppComponent,
    MainViewComponent
  ],
  imports: [
    BrowserModule,
    CommonModule,
    RouterModule.forRoot(routes),
    BrowserAnimationsModule,
    StoreModule.forRoot({}, {}),
    EffectsModule.forRoot([]),
  ],
  providers: [
    provideHttpClient(
      withInterceptorsFromDi(),
    )
  ],
  bootstrap: [AppComponent]
})
export class CoreModule { }
