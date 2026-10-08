import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-loading-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<div class="loading-state"><span></span><span></span><span></span></div>`,
  styles: [
    `
      .loading-state {
        display: flex;
        gap: 0.35rem;
        justify-content: center;
        padding: 2.5rem;
      }
      span {
        width: 0.5rem;
        height: 0.5rem;
        border-radius: 50%;
        background: var(--accent);
        animation: pulse 1s infinite alternate;
      }
      span:nth-child(2) {
        animation-delay: 0.2s;
      }
      span:nth-child(3) {
        animation-delay: 0.4s;
      }
      @keyframes pulse {
        to {
          opacity: 0.25;
          transform: translateY(-0.3rem);
        }
      }
    `,
  ],
})
export class LoadingStateComponent {}
