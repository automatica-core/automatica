import { Directive, HostListener } from "@angular/core";

@Directive({
  selector: "[navDropdown]",
  // tslint:disable-next-line:use-host-property-decorator
  host: {
    "[class.open]": "_open",
  }
})
export class NavDropdownDirective {

  private _open = false;

  /**
  * Checks if the dropdown menu is open or not.
  */
  isOpen() { return this._open; }

  /**
  * Opens the dropdown menu.
  */
  open() {
    this._open = true;
  }

  /**
  * Closes the dropdown menu .
  */
  close() {
    this._open = false;
  }

  /**
  * Toggles the dropdown menu.
  */
  toggle() {
    if (this.isOpen()) {
      this.close();
    } else {
      this.open();
    }
  }
}

/**
* Allows the dropdown to be toggled via click.
*/
@Directive({
  selector: "[navDropdownToggle]",
})
export class NavDropdownToggleDirective {
  constructor(private dropdown: NavDropdownDirective) { }

  @HostListener("click", ["$event"])
  toggleOpen($event: any) {
     $event.preventDefault();
     this.dropdown.toggle();
  }
}

// export const NGB_DROPDOWN_DIRECTIVES = [NgbDropdownToggle, NgbDropdown];
