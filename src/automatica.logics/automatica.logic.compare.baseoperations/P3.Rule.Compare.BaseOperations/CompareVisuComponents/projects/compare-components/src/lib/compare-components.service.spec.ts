import { TestBed, inject } from '@angular/core/testing';

import { CompareComponentsService } from './compare-components.service';

describe('CompareComponentsService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CompareComponentsService]
    });
  });

  it('should be created', inject([CompareComponentsService], (service: CompareComponentsService) => {
    expect(service).toBeTruthy();
  }));
});
