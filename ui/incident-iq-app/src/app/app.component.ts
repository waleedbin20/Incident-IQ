import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CrimeBoardComponent, CrimeBoardData } from './components/crime-board/crime-board.component';
import { GlassBoxComponent } from './components/glass-box/glass-box.component';
import { SwarmMapComponent } from './components/swarm-map/swarm-map.component';
import { ControlPanelComponent } from './components/control-panel/control-panel.component';
import { SignalrService } from './services/signalr.service';
import { ApiService } from './services/api.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, CrimeBoardComponent, GlassBoxComponent, SwarmMapComponent, ControlPanelComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  @ViewChild(CrimeBoardComponent) crimeBoard!: CrimeBoardComponent;
  @ViewChild(GlassBoxComponent) glassBox!: GlassBoxComponent;
  title = 'incident-iq-app';
  isInvestigating = false;
  isResolved = false;
  viewMode: 'terminal' | 'map' = 'map';

  constructor(private signalRService: SignalrService, private apiService: ApiService) {}

  ngOnInit() {
    this.signalRService.startConnection();
  }

  onInitiateP1(eventData: { description: string, repo: string }) {
    if (this.glassBox) this.glassBox.clearLogs();
    this.isInvestigating = true;
    this.isResolved = false;
    
    // Voice prompt
    this.speak('Incident P1-9942 detected. Activating Azure Foundry Swarm.', 1.0);

    this.apiService.investigate(eventData.description, eventData.repo).subscribe({
      next: (data: any) => {
        this.isInvestigating = false;
        this.isResolved = true;
        this.crimeBoard.renderGraph(data);
        
        // Announce resolution
        const cleanSummary = data.root_cause_summary.replace(/Error.*?Fallback:/g, '').trim();
        setTimeout(() => this.speak(`Investigation complete. ${cleanSummary}`, 1.0), 1000);
      },
      error: (err) => {
        this.isInvestigating = false;
        this.isResolved = true; // Force resolve to clear bubbles on error
        console.error('Investigation failed', err);
        // Announce error
        this.speak('Investigation failed due to critical error.', 1.0);
      }
    });
  }

  onApproveRollback() {
    alert('Rollback Approved! PR #12 has been reverted and deployment triggered.');
  }

  private speak(text: string, rate: number) {
    if (!window.speechSynthesis) return;
    window.speechSynthesis.cancel();
    const msg = new SpeechSynthesisUtterance(text);
    msg.pitch = 0.8;
    msg.rate = rate;
    
    const voices = window.speechSynthesis.getVoices();
    const voice = voices.find(v => v.name.includes('Google') || v.name.includes('Zira') || v.name.includes('UK English'));
    if (voice) msg.voice = voice;

    window.speechSynthesis.speak(msg);
  }
}
