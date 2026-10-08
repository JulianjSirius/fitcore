import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="empty-state">
      <span class="empty-state__mark" aria-hidden="true"></span>
      <h2>{{ title() }}</h2>
      <p>{{ message() }}</p>
    </div>
  `,
  styles: [
    `
      .empty-state {
        display: grid;
        justify-items: center;
        gap: 0.4rem;
        padding: 2.5rem 1.5rem;
        border: 1px dashed var(--border-strong);
        border-radius: var(--radius);
        text-align: center;
        color: var(--muted);
      }
      .empty-state__mark {
        width: 34px;
        height: 4px;
        border-radius: 2px;
        background: var(--accent);
        margin-bottom: 0.4rem;
      }
      h2 {
        color: var(--text);
        font-size: 1rem;
      }
      p {
        margin: 0;
        font-size: 0.9rem;
      }
    `,
  ],
})
export class EmptyStateComponent {
  readonly title = input('Sin resultados');
  readonly message = input('Todavía no hay información para mostrar.');
}
