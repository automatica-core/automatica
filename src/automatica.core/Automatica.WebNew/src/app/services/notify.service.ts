import { Injectable } from "@angular/core";
import notify from "devextreme/ui/notify";
import { L10nTranslationService } from "angular-l10n";

@Injectable()
export class NotifyService {


    constructor(private translate: L10nTranslationService) {

    }
    notifyInfo(text: any): any {
        notify(this.translate.translate(text), "info", 2000);
    }
    notifySuccess(text: string) {
        notify(this.translate.translate(text), "success", 2000);
    }
    notifyWarning(text: string, timeout = 2000) {
        notify(this.translate.translate(text), "warning", timeout);
    }

    notifyError(error: string, timeout = 5000) {
        try {
        notify(this.translate.translate(error), "error", timeout);
        }
        catch(e) {
            notify(this.translate.translate("ERROR.UNKNOWN"), "error", timeout);
        }
    }
}
