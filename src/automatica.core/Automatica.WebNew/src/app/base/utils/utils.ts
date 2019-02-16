import { DxValidatorComponent } from "devextreme-angular";

export class InputValidator {
    private validateOther: boolean = false;

    /**
     * Cuts decimal places of a number.
     * @param value Value to be transformed
     */
    public cutDecimalPlaces(value: number) {
        // Cut decimal places as suggested by DevExtreme
        if (value && value.toString().indexOf(".") !== -1) {
            value = parseInt(value.toString(), 10);
        }
        return value;
    }

    /**
     * Validates min/max date inputs.
     * @param event Validation event
     * @param startDate Start date
     * @param endDate End date
     * @param startDateValidator Start date dx validator component
     * @param endDateValidator End date dx validator component
     */
    public validateDates(event: any, startDate: Date, endDate: Date, startDateValidator: DxValidatorComponent, endDateValidator: DxValidatorComponent) {
        return this.validate(event, startDateValidator, endDateValidator, () => {
            return endDate > startDate;
        });
    }

    /**
     * Validates min/max value inputs.
     * @param event Validation event
     * @param startDate Min value
     * @param endDate Max value
     * @param startDateValidator Min value dx validator component
     * @param endDateValidator Max value dx validator component
     */
    public validateMinMax(event: any, minValidValue: number, maxValidValue: number, minValueValidator: DxValidatorComponent, maxValueValidator: DxValidatorComponent) {
        return this.validate(event, minValueValidator, maxValueValidator, () => {
            return maxValidValue > minValidValue;
        });
    }

    /**
     * Internally validates two inputs.
     * @param event Validation event
     * @param validatorA Validator A
     * @param validatorB Validator B
     * @param condition Condition that determines whether or not the inputs are valid.
     */
    private validate(event: any, validatorA: DxValidatorComponent, validatorB: DxValidatorComponent, condition: () => boolean) {
        if (this.validateOther) {
            this.validateOther = false;
            if (event.validator === validatorA.instance) {
                validatorB.instance.validate();
            } else {
                validatorA.instance.validate();
            }
        }

        this.validateOther = true;
        return condition();
    }
}
