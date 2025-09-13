import { CUSTOM_ELEMENTS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { RouterModule } from "@angular/router";
import { EffectsModule } from "@ngrx/effects";
import { StoreModule } from "@ngrx/store";
import { MainViewComponent } from "../..";

describe('MainViewComponent', () => {
    let component: MainViewComponent;
    let fixture: ComponentFixture<MainViewComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [MainViewComponent],
            imports: [
                RouterModule.forRoot([]),
                StoreModule.forRoot({}, { runtimeChecks: { strictStateImmutability: true, strictActionImmutability: true } }),
                EffectsModule.forRoot([]),
            ],
            providers: [
            ],
            schemas: [CUSTOM_ELEMENTS_SCHEMA]
        }).compileComponents();
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(MainViewComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});