import { Component, OnInit, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SignalrService, TraceLog } from '../../services/signalr.service';

@Component({
  selector: 'app-glass-box',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './glass-box.component.html',
  styleUrls: ['./glass-box.component.scss']
})
export class GlassBoxComponent implements OnInit, AfterViewChecked {
  @ViewChild('terminalBody') private terminalBody!: ElementRef;
  public logs: TraceLog[] = [];

  private isAutoScrollPaused = false;

  constructor(private signalRService: SignalrService) {}

  ngOnInit() {
    this.signalRService.trace$.subscribe(trace => {
      // If the level is EXECUTE (Tool Call) or action is ORCHESTRATOR, push a new line
      if (trace.action === 'ORCHESTRATOR' || trace.level === 'EXECUTE') {
        // override action for UI formatting
        if (trace.level === 'EXECUTE') trace.action = 'EXECUTE';
        this.logs.push(trace);
        return;
      }

      // If it's a normal agent message, check if the last log was from the same agent
      if (this.logs.length > 0) {
        const lastLog = this.logs[this.logs.length - 1];
        if (lastLog.action === trace.action && lastLog.level !== 'EXECUTE') {
          lastLog.message += trace.message;
          return;
        }
      }
      
      // Otherwise, it's a new agent's turn
      this.logs.push(trace);
    });
  }

  onMouseEnter() {
    this.isAutoScrollPaused = true;
  }

  onMouseLeave() {
    this.isAutoScrollPaused = false;
    this.scrollToBottom(); // Immediately snap to bottom when mouse leaves
  }

  ngAfterViewChecked() {
    this.scrollToBottom();
  }

  private scrollToBottom(): void {
    if (this.isAutoScrollPaused) return;

    try {
      this.terminalBody.nativeElement.scrollTop = this.terminalBody.nativeElement.scrollHeight;
    } catch(err) { }
  }

  public clearLogs(): void {
    this.logs = [];
  }
}
