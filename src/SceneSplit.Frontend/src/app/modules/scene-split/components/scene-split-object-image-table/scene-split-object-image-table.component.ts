import { Component, OnInit } from '@angular/core';
import { Store } from '@ngrx/store';
import { Observable, of } from 'rxjs';
import { getObjectImages, ObjectImage, selectObjectImages } from '../..';

@Component({
  selector: 'app-scene-split-object-image-table',
  templateUrl: './scene-split-object-image-table.component.html',
  styleUrl: './scene-split-object-image-table.component.scss'
})
export class SceneSplitObjectImageTableComponent implements OnInit {
  images$: Observable<ObjectImage[]> = of([]);

  constructor(private readonly store: Store) { }

  ngOnInit(): void {
    this.images$ = this.store.select(selectObjectImages);
    this.store.dispatch(getObjectImages());
  }
}
