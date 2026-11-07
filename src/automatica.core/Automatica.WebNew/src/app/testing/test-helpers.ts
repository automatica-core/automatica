import { Directive, EventEmitter, Input, OnInit, Pipe, PipeTransform, TemplateRef, ViewContainerRef } from "@angular/core";
import { L10nTranslationService } from "angular-l10n";
import { NotifyService } from "../services/notify.service";

/**
 * Test doubles shared by the specs. Only specs import this folder; the app build starts at main.ts and never sees it.
 */

/** renders the translation key (plus the params) instead of loading the locale files */
@Pipe({ name: "translateAsync", standalone: true, pure: false })
export class FakeTranslateAsyncPipe implements PipeTransform {
    transform(key: any, params?: any): string {
        return params ? `${key} ${JSON.stringify(params)}` : key;
    }
}

@Pipe({ name: "translate", standalone: true })
export class FakeTranslatePipe implements PipeTransform {
    transform(key: any, params?: any): string {
        return params ? `${key} ${JSON.stringify(params)}` : key;
    }
}

/**
 * Stands in for DevExtreme's dxTemplate: a bare `*dxTemplate` is rendered in place (so the elements a dx widget would host exist),
 * named templates (`*dxTemplate="let x of 'item'"`) stay unrendered, their context only exists inside the widget.
 */
@Directive({ selector: "[dxTemplate]", standalone: true })
export class FakeDxTemplateDirective implements OnInit {
    @Input() dxTemplateOf: string;

    constructor(private view: ViewContainerRef, private template: TemplateRef<any>) { }

    ngOnInit() {
        if (!this.dxTemplateOf) {
            this.view.createEmbeddedView(this.template);
        }
    }
}

export const FAKE_TEMPLATE_IMPORTS = [FakeTranslateAsyncPipe, FakeTranslatePipe];

/** translate returns the key (or the mapped text), so assertions can check which key was used */
export function createTranslateSpy(texts: { [key: string]: string } = {}): jasmine.SpyObj<L10nTranslationService> {
    const spy = jasmine.createSpyObj<L10nTranslationService>("L10nTranslationService", ["translate", "getLocale", "setLocale"]);
    spy.translate.and.callFake(((key: string) => texts[key] ?? key) as any);
    spy.getLocale.and.returnValue({ language: "en" } as any);
    spy.setLocale.and.returnValue(Promise.resolve());
    return spy;
}

export function createNotifySpy(): jasmine.SpyObj<NotifyService> {
    return jasmine.createSpyObj<NotifyService>("NotifyService", ["notifyInfo", "notifySuccess", "notifyWarning", "notifyError"]);
}

/** resolves after the browser rendered twice: ResizeObserver callbacks and layout have run by then */
export function nextFrames(count = 2): Promise<void> {
    return new Promise(resolve => {
        const step = (left: number) => left <= 0 ? resolve() : requestAnimationFrame(() => step(left - 1));
        step(count);
    });
}

/** lets the pending promise callbacks run (services faked with resolved promises) */
export async function flushPromises(rounds = 5) {
    for (let i = 0; i < rounds; i++) {
        await Promise.resolve();
    }
}

export function pointer(type: string, init: PointerEventInit = {}): PointerEvent {
    return new PointerEvent(type, { bubbles: true, cancelable: true, button: 0, buttons: 1, pointerId: 1, ...init });
}

export function key(name: string, init: KeyboardEventInit = {}): KeyboardEvent {
    return new KeyboardEvent("keydown", { key: name, bubbles: true, cancelable: true, ...init });
}

/** an EventEmitter based fake for the hub/engine services, the events are real so subscriptions behave like in the app */
export function emitter<T = any>() {
    return new EventEmitter<T>();
}

/** removes the given localStorage keys for the test and returns the function that puts the previous values back */
export function isolateLocalStorage(...keys: string[]): () => void {
    const saved = keys.map(k => [k, localStorage.getItem(k)] as const);
    keys.forEach(k => localStorage.removeItem(k));
    return () => saved.forEach(([k, v]) => v === null ? localStorage.removeItem(k) : localStorage.setItem(k, v));
}

/** a promise that is rejected but already has a handler, so it does not log an unhandled rejection before the code under test awaits it */
export function rejected<T = any>(error: any): Promise<T> {
    const p = Promise.reject(error);
    p.catch(() => void 0);
    return p;
}
