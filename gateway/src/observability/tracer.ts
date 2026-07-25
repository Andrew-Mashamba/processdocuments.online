/**
 * Distributed Tracing
 * Week 12: Production Polish - OpenTelemetry Integration
 */

import { v4 as uuidv4 } from 'uuid';

export interface Span {
  trace_id: string;
  span_id: string;
  parent_span_id?: string;
  name: string;
  start_time: number;
  end_time?: number;
  duration_ms?: number;
  attributes: Record<string, any>;
  events: SpanEvent[];
  status: 'ok' | 'error';
  error?: {
    message: string;
    stack?: string;
  };
}

export interface SpanEvent {
  timestamp: number;
  name: string;
  attributes?: Record<string, any>;
}

export class Tracer {
  private spans: Map<string, Span> = new Map();
  private serviceName: string;

  constructor(serviceName: string = 'zima-gateway') {
    this.serviceName = serviceName;
  }

  /**
   * Start a new trace
   */
  startTrace(name: string, attributes: Record<string, any> = {}): Span {
    const trace_id = this.generateTraceId();
    const span_id = this.generateSpanId();

    const span: Span = {
      trace_id,
      span_id,
      name,
      start_time: Date.now(),
      attributes: {
        service: this.serviceName,
        ...attributes
      },
      events: [],
      status: 'ok'
    };

    this.spans.set(span_id, span);
    return span;
  }

  /**
   * Start a child span
   */
  startSpan(name: string, parent: Span, attributes: Record<string, any> = {}): Span {
    const span_id = this.generateSpanId();

    const span: Span = {
      trace_id: parent.trace_id,
      span_id,
      parent_span_id: parent.span_id,
      name,
      start_time: Date.now(),
      attributes: {
        service: this.serviceName,
        ...attributes
      },
      events: [],
      status: 'ok'
    };

    this.spans.set(span_id, span);
    return span;
  }

  /**
   * End a span
   */
  endSpan(span: Span, status: 'ok' | 'error' = 'ok', error?: Error): void {
    span.end_time = Date.now();
    span.duration_ms = span.end_time - span.start_time;
    span.status = status;

    if (error) {
      span.error = {
        message: error.message,
        stack: error.stack
      };
    }

    // Emit span (could send to OpenTelemetry collector here)
    this.emitSpan(span);
  }

  /**
   * Add event to span
   */
  addEvent(span: Span, name: string, attributes?: Record<string, any>): void {
    span.events.push({
      timestamp: Date.now(),
      name,
      attributes
    });
  }

  /**
   * Set span attribute
   */
  setAttribute(span: Span, key: string, value: any): void {
    span.attributes[key] = value;
  }

  /**
   * Trace an async function
   */
  async trace<T>(
    name: string,
    fn: (span: Span) => Promise<T>,
    parent?: Span,
    attributes?: Record<string, any>
  ): Promise<T> {
    const span = parent
      ? this.startSpan(name, parent, attributes)
      : this.startTrace(name, attributes);

    try {
      const result = await fn(span);
      this.endSpan(span, 'ok');
      return result;
    } catch (error: any) {
      this.endSpan(span, 'error', error);
      throw error;
    }
  }

  /**
   * Get span by ID
   */
  getSpan(span_id: string): Span | undefined {
    return this.spans.get(span_id);
  }

  /**
   * Get all spans for a trace
   */
  getTrace(trace_id: string): Span[] {
    return Array.from(this.spans.values()).filter(s => s.trace_id === trace_id);
  }

  private generateTraceId(): string {
    return uuidv4().replace(/-/g, '');
  }

  private generateSpanId(): string {
    return uuidv4().replace(/-/g, '').substring(0, 16);
  }

  private emitSpan(span: Span): void {
    // In production, send to OpenTelemetry collector
    // For now, log to console in debug mode
    if (process.env.DEBUG_TRACING) {
      console.log('[TRACE]', JSON.stringify(span, null, 2));
    }
  }
}

// Global tracer instance
let globalTracer: Tracer;

export function getTracer(serviceName?: string): Tracer {
  if (!globalTracer) {
    globalTracer = new Tracer(serviceName);
  }
  return globalTracer;
}

/**
 * Express middleware for automatic tracing
 */
export function tracingMiddleware(tracer: Tracer) {
  return (req: any, res: any, next: any) => {
    const span = tracer.startTrace(`HTTP ${req.method} ${req.path}`, {
      'http.method': req.method,
      'http.path': req.path,
      'http.user_agent': req.get('user-agent')
    });

    // Attach span to request
    req.span = span;
    req.trace_id = span.trace_id;

    // End span when response finishes
    res.on('finish', () => {
      tracer.setAttribute(span, 'http.status_code', res.statusCode);
      tracer.endSpan(span, res.statusCode >= 400 ? 'error' : 'ok');
    });

    next();
  };
}
