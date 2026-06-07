import { Component, OnInit, OnDestroy, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SignalrService, TraceLog } from '../../services/signalr.service';
import { Subscription } from 'rxjs';

interface AgentState {
  id: string;
  label: string;
  icon: string;
  latestMessage: string;
  isTyping: boolean;
  positionClass: string;
}

@Component({
  selector: 'app-swarm-map',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './swarm-map.component.html',
  styleUrls: ['./swarm-map.component.scss']
})
export class SwarmMapComponent implements OnInit, OnDestroy, OnChanges {
  @Input() isInvestigating = false;
  @Input() isResolved = false;
  
  private sub!: Subscription;
  
  public agents: AgentState[] = [
    { id: 'Commander', label: 'Commander', icon: '🧠', latestMessage: '', isTyping: false, positionClass: 'pos-top' },
    { id: 'Infra-Agent', label: 'Infra', icon: '⚙️', latestMessage: '', isTyping: false, positionClass: 'pos-left' },
    { id: 'Support-Agent', label: 'Support', icon: '🎧', latestMessage: '', isTyping: false, positionClass: 'pos-bottom' },
    { id: 'DevOps-Agent', label: 'DevOps', icon: '🛠️', latestMessage: '', isTyping: false, positionClass: 'pos-right' }
  ];

  public activeTool: { name: string, active: boolean, sourceClass: string } | null = null;
  public dataPacket: { active: boolean, sourceClass: string } = { active: false, sourceClass: '' };

  public traceQueue: TraceLog[] = [];
  public isProcessingQueue = false;
  public isAwaitingApproval = false;
  public hasApproved = false;

  constructor(private signalRService: SignalrService) {}

  ngOnChanges(changes: SimpleChanges) {
    if (changes['isResolved'] && changes['isResolved'].currentValue === true) {
      this.traceQueue = [];
      this.agents.forEach(a => a.isTyping = false);
      this.isProcessingQueue = false;
    }
  }

  ngOnInit() {
    this.sub = this.signalRService.trace$.subscribe(trace => {
      this.traceQueue.push(trace);
      this.processQueue();
    });
  }

  ngOnDestroy() {
    if (this.sub) this.sub.unsubscribe();
  }

  private sleep(ms: number) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  async processQueue() {
    if (this.isProcessingQueue || this.isAwaitingApproval) return;
    this.isProcessingQueue = true;

    while (this.traceQueue.length > 0) {
      if (this.isAwaitingApproval) break;
      const trace = this.traceQueue.shift()!;
      
      // HitL Trigger Hack for Demo:
      // If Commander calls a specific tool or we just detect a high-risk action
      if (trace.message.includes('QueryAppInsightsAsync') && !this.hasApproved) {
        this.isAwaitingApproval = true;
        this.activeTool = null;
        this.agents.forEach(a => a.isTyping = false);
        this.traceQueue.unshift(trace); // put it back for when we resume
        
        // Voice alert
        this.speak('Authorization Required. Awaiting human override.', 1.0);
        break;
      }
      
      this.handleTrace(trace);
      
      // Artificial delay for cinematic effect so they don't instantly blast through
      await this.sleep(400); 
    }
    this.isProcessingQueue = false;
  }

  public approveAction() {
    this.isAwaitingApproval = false;
    this.hasApproved = true;
    this.speak('Override Authorized. Resuming operations.', 1.1);
    this.processQueue();
  }

  private handleTrace(trace: TraceLog) {
    let sourceId = 'ORCHESTRATOR';
    if (trace.action.includes('Commander')) sourceId = 'Commander';
    else if (trace.action.includes('Infra')) sourceId = 'Infra-Agent';
    else if (trace.action.includes('Support')) sourceId = 'Support-Agent';
    else if (trace.action.includes('DevOps')) sourceId = 'DevOps-Agent';

    const agent = this.agents.find(a => a.id === sourceId);

    if (trace.level === 'EXECUTE' && agent) {
      const toolMatch = trace.message.match(/Calling Tool: (.*?)\(/);
      const toolName = toolMatch ? toolMatch[1] : 'External System';
      
      this.activeTool = { name: toolName, active: true, sourceClass: agent.positionClass };
      this.playBeep();
      setTimeout(() => {
        if (this.activeTool?.name === toolName) this.activeTool = null;
      }, 2000);
    } 
    else if (agent) {
      agent.latestMessage = trace.message;
      agent.isTyping = true;
      
      this.dataPacket = { active: false, sourceClass: agent.positionClass };
      setTimeout(() => this.dataPacket.active = true, 50);

      setTimeout(() => {
        agent.isTyping = false;
      }, 3000);
    }
  }

  private playBeep() {
    try {
      const ctx = new (window.AudioContext || (window as any).webkitAudioContext)();
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.connect(gain);
      gain.connect(ctx.destination);
      osc.type = 'sine';
      osc.frequency.setValueAtTime(800, ctx.currentTime);
      osc.frequency.exponentialRampToValueAtTime(1200, ctx.currentTime + 0.1);
      gain.gain.setValueAtTime(0.05, ctx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.1);
      osc.start();
      osc.stop(ctx.currentTime + 0.1);
    } catch(e) {}
  }

  private speak(text: string, rate: number) {
    if (!window.speechSynthesis) return;
    window.speechSynthesis.cancel();
    const msg = new SpeechSynthesisUtterance(text);
    msg.pitch = 0.8;
    msg.rate = rate;
    
    // Try to find a robotic/Google voice
    const voices = window.speechSynthesis.getVoices();
    const voice = voices.find(v => v.name.includes('Google') || v.name.includes('Zira') || v.name.includes('UK English'));
    if (voice) msg.voice = voice;

    window.speechSynthesis.speak(msg);
  }
}
