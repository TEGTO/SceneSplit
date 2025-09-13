import { TestBed } from "@angular/core/testing";
import { MatSnackBar } from "@angular/material/snack-bar";
import { ErrorAnnotatedComponent, InfoAnnotatedComponent } from "../..";
import { SnackbarManager } from "./snackbar-manager.service";

describe('SnackbarManager', () => {
    let mockSnackBar: jasmine.SpyObj<MatSnackBar>;
    let service: SnackbarManager;

    beforeEach(() => {
        mockSnackBar = jasmine.createSpyObj('SnackbarManager', ['openFromComponent']);
        TestBed.configureTestingModule({
            providers: [
                SnackbarManager,
                { provide: MatSnackBar, useValue: mockSnackBar },
            ]
        });
        service = TestBed.inject(SnackbarManager);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should openFromComponent be called with InfoAnnotatedComponent', () => {
        const message = "message";
        const durationInSeconds = 5;
        service.openInfoSnackbar(message, durationInSeconds);
        expect(mockSnackBar.openFromComponent).toHaveBeenCalledWith(InfoAnnotatedComponent, {
            duration: durationInSeconds * 1000,
            data: {
                message: message
            }
        });
    });

    it('should openFromComponent be called with ErrorAnnotatedComponent', () => {
        const message = ["message"];
        service.openErrorSnackbar(message);
        expect(mockSnackBar.openFromComponent).toHaveBeenCalledWith(ErrorAnnotatedComponent, {
            duration: service.errorDurationInSeconds * 1000,
            data: {
                messages: message
            }
        });
    });
});