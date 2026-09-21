import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';

describe('App', () => {
  it('opens the personal workspace through the main navigation', async () => {
    await TestBed.configureTestingModule({imports:[App],providers:[provideRouter([])]}).compileComponents();
    const fixture=TestBed.createComponent(App);fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('a[href="/personal"]').textContent).toContain('hisab');
    expect(fixture.nativeElement.querySelector('nav').textContent).toContain('My expenses');
  });
});
