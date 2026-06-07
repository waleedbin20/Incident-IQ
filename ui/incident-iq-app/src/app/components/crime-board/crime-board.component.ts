import { Component, ElementRef, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import * as d3 from 'd3';

export interface GraphNode {
  id: string;
  type: string;
  label: string;
}

export interface GraphEdge {
  source: string | any;
  target: string | any;
  label: string;
}

export interface CrimeBoardData {
  incident_id: string;
  root_cause_summary: string;
  nodes: GraphNode[];
  edges: GraphEdge[];
  remediation: {
    action: string;
    requires_approval: boolean;
  };
}

@Component({
  selector: 'app-crime-board',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './crime-board.component.html',
  styleUrls: ['./crime-board.component.scss']
})
export class CrimeBoardComponent implements AfterViewInit {
  @ViewChild('svgContainer', { static: true }) svgContainer!: ElementRef;
  
  public data: any | null = null;

  public renderGraph(data: any) {
    if (!data) return;
    if (typeof data === 'string') {
      try {
        // Extract the last valid JSON object from the string just in case the LLM injected garbage
        const match = data.match(/\{[\s\S]*("nodes"|"Nodes")[\s\S]*\}/);
        if (match) {
          data = match[0];
        }
        data = JSON.parse(data);
      } catch (e) {
        console.error('Failed to parse graph data', e);
        return;
      }
    }

    this.data = data;
    const width = this.svgContainer.nativeElement.clientWidth || 800;
    const height = this.svgContainer.nativeElement.clientHeight || 500;

    d3.select(this.svgContainer.nativeElement).selectAll('*').remove();

    // Sanitize data
    const nodes = data.nodes || data.Nodes || [];
    const nodeIds = new Set(nodes.map((n: any) => n.id));
    const rawEdges = data.edges || data.Edges || [];
    const edges = rawEdges.filter((e: any) => nodeIds.has(e.source as string) && nodeIds.has(e.target as string));

    if (nodes.length === 0) {
      console.warn('No nodes to render');
      return;
    }

    const svg = d3.select(this.svgContainer.nativeElement)
      .append('svg')
      .attr('width', '100%')
      .attr('height', '100%')
      .attr('viewBox', `0 0 ${width} ${height}`);

    const simulation = d3.forceSimulation(nodes as any)
      .force('link', d3.forceLink(edges).id((d: any) => d.id).distance(100))
      .force('charge', d3.forceManyBody().strength(-600))
      .force('center', d3.forceCenter((width / 2) - 180, height / 2))
      .force('collide', d3.forceCollide().radius(80))
      .force('y', d3.forceY((d: any) => {
        if (d.type === 'user_report') return 100;
        if (d.type === 'server') return height / 2;
        return height - 100;
      }).strength(0.8))
      .force('x', d3.forceX((width / 2) - 180).strength(0.1));

    // Glow filter
    const defs = svg.append('defs');
    const filter = defs.append('filter').attr('id', 'glow');
    filter.append('feGaussianBlur').attr('stdDeviation', '4').attr('result', 'coloredBlur');
    const feMerge = filter.append('feMerge');
    feMerge.append('feMergeNode').attr('in', 'coloredBlur');
    feMerge.append('feMergeNode').attr('in', 'SourceGraphic');

    const link = svg.append('g')
      .selectAll('line')
      .data(edges)
      .join('line')
      .attr('class', 'link')
      .attr('stroke', '#00f0ff')
      .attr('stroke-opacity', 0.6)
      .attr('stroke-width', 3);

    const node = svg.append('g')
      .selectAll('g')
      .data(nodes)
      .join('g')
      .call(d3.drag()
        .on('start', (event, d: any) => {
          if (!event.active) simulation.alphaTarget(0.3).restart();
          d.fx = d.x; d.fy = d.y;
        })
        .on('drag', (event, d: any) => {
          d.fx = event.x; d.fy = event.y;
        })
        .on('end', (event, d: any) => {
          if (!event.active) simulation.alphaTarget(0);
          d.fx = null; d.fy = null;
        }) as any);

    // Node circles with cyber colors
    node.append('circle')
      .attr('r', 30)
      .attr('fill', '#161b22')
      .attr('stroke', (d: any) => d.type === 'user_report' ? '#ff2a55' : d.type === 'server' ? '#00f0ff' : '#39ff14')
      .attr('stroke-width', 3)
      .style('filter', 'url(#glow)');

    // Node Icons
    node.append('text')
      .text((d: any) => d.type === 'user_report' ? '🚨' : d.type === 'server' ? '🖥️' : '🔀')
      .attr('text-anchor', 'middle')
      .attr('dominant-baseline', 'central')
      .style('font-size', '24px');

    // Node Labels
    node.append('text')
      .text((d: any) => d.label)
      .attr('class', 'node-text')
      .attr('y', 45)
      .attr('text-anchor', 'middle')
      .attr('fill', '#e2e8f0')
      .style('font-size', '14px');

    simulation.on('tick', () => {
      link
        .attr('x1', (d: any) => d.source.x)
        .attr('y1', (d: any) => d.source.y)
        .attr('x2', (d: any) => d.target.x)
        .attr('y2', (d: any) => d.target.y);

      node
        .attr('transform', (d: any) => `translate(${d.x},${d.y})`);
    });
  }

  ngAfterViewInit() { }
}
