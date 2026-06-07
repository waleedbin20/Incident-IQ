import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-control-panel',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './control-panel.component.html',
  styleUrls: ['./control-panel.component.scss']
})
export class ControlPanelComponent {
  @Output() initiate = new EventEmitter<{description: string, repo: string}>();
  @Output() rollback = new EventEmitter<void>();

  public descriptionText: string = 'Users are reporting checkout timeouts and 503 errors across the platform.';
  
  public repositories: string[] = [
    'microsoft/TypeScript',
    'dotnet/aspnetcore',
    'angular/angular',
    'facebook/react',
    'microsoft/semantic-kernel'
  ];
  public selectedRepo: string = this.repositories[0];

  onInitiate() {
    this.initiate.emit({ description: this.descriptionText, repo: this.selectedRepo });
  }

  onRollback() {
    this.rollback.emit();
  }
}
