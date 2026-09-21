import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { ReceiptsService } from './receipts.service';

@Component({
  selector: 'app-receipt',
  standalone: true,
  imports: [RouterLink, DecimalPipe],
  templateUrl: './receipt.page.html',
  styleUrl: './receipt.page.scss',
})
export class ReceiptPage implements OnInit {
  readonly svc = inject(ReceiptsService);
  private readonly route = inject(ActivatedRoute);

  ngOnInit(): void {
    const paymentId = +(this.route.snapshot.paramMap.get('paymentId') ?? 0);
    this.svc.load(paymentId);
  }

  download(): void {
    this.svc.downloadHtml();
  }

  print(): void {
    this.svc.print();
  }

  modeLabel(mode: string): string {
    return this.svc.modeLabel(mode);
  }
}
