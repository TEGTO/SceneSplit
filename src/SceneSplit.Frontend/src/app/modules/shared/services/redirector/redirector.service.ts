import { Injectable } from '@angular/core';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class RedirectorService {

  constructor(
    private readonly router: Router
  ) { }

  redirectToHome(): void {
    this.router.navigate(['']);
  }

  redirectTo(path: string): void {
    this.router.navigate([path]);
  }

  redirectToExternalUrl(url: string): void {
    window.location.href = url;
  }
}
