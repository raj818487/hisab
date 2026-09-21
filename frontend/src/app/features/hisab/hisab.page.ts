import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { HisabService } from './hisab.service';

@Component({
  selector: 'app-hisab',
  standalone: true,
  imports: [FormsModule, DecimalPipe],
  templateUrl: './hisab.page.html',
  styleUrl: './hisab.page.scss'
})
export class HisabPage implements OnInit {
  readonly svc = inject(HisabService);

  ngOnInit(): void {
    this.svc.load();
  }
}
