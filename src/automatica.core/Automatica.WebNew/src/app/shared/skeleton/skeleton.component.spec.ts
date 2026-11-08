import { ChangeDetectionStrategy, Component } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { SkeletonComponent, SkeletonVariant } from "./skeleton.component";
import { FAKE_TEMPLATE_IMPORTS } from "../../testing/test-helpers";

@Component({
    // components default to OnPush, the host is changed between renders
    changeDetection: ChangeDetectionStrategy.Eager,
    template: `<app-skeleton [variant]="variant" [rows]="rows" [columns]="columns"></app-skeleton>`,
    standalone: false
})
class HostComponent {
    variant: SkeletonVariant = "list";
    rows = 3;
    columns = 4;
}

describe("SkeletonComponent", () => {
    let fixture: ComponentFixture<HostComponent>;
    let host: HostComponent;

    const el = () => fixture.nativeElement.querySelector("app-skeleton") as HTMLElement;
    const render = (variant: SkeletonVariant, rows = 3, columns = 4) => {
        host.variant = variant;
        host.rows = rows;
        host.columns = columns;
        fixture.detectChanges();
        return el();
    };

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [HostComponent, SkeletonComponent],
            imports: [...FAKE_TEMPLATE_IMPORTS]
        }).compileComponents();

        fixture = TestBed.createComponent(HostComponent);
        host = fixture.componentInstance;
    });

    it("is a busy status region with a readable label and hides the blocks from screen readers", () => {
        const sk = render("list");
        expect(sk.getAttribute("role")).toBe("status");
        expect(sk.getAttribute("aria-busy")).toBe("true");
        expect(sk.querySelector(".sk-label").textContent).toContain("UI.LOADING");
        sk.querySelectorAll(".sk-row").forEach(r => expect(r.getAttribute("aria-hidden")).toBe("true"));
    });

    it("renders one list row per requested row", () => {
        expect(render("list", 5).querySelectorAll(".sk-li").length).toBe(5);
    });

    it("renders a header plus rows with the requested columns for tables", () => {
        const sk = render("table", 2, 3);
        expect(sk.getAttribute("data-variant")).toBe("table");
        expect(sk.querySelectorAll(".sk-head .sk-cell").length).toBe(3);
        expect(sk.querySelectorAll(".sk-row:not(.sk-head)").length).toBe(2);
        expect(sk.querySelectorAll(".sk-row:not(.sk-head) .sk-cell").length).toBe(6);
    });

    it("indents tree rows", () => {
        const rows = Array.from(render("tree", 4).querySelectorAll<HTMLElement>(".sk-row"));
        expect(rows.length).toBe(4);
        expect(new Set(rows.map(r => r.style.paddingLeft)).size).toBeGreaterThan(1);
    });

    it("renders form fields, cards and text lines", () => {
        expect(render("form", 3).querySelectorAll(".sk-field .sk-input").length).toBe(3);
        expect(render("cards", 4).querySelectorAll(".sk-card").length).toBe(4);
        const lines = render("lines", 3).querySelectorAll<HTMLElement>(".sk-line");
        expect(lines.length).toBe(3);
        expect(lines[2].style.width).toBe("45%");
    });

    it("treats invalid row counts as none", () => {
        expect(render("list", -2).querySelectorAll(".sk-li").length).toBe(0);
        expect(render("list", NaN).querySelectorAll(".sk-li").length).toBe(0);
    });
});
