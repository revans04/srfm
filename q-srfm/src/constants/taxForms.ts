import type { TaxForm } from '../types';
import { EntityType } from '../types';

export const DEFAULT_TAX_FORMS: TaxForm[] = [
  {
    id: 'irs_schedule_e',
    name: 'Schedule E',
    description: 'Supplemental Income and Loss (Rentals)',
    applicableEntityTypes: [EntityType.RentalProperty],
    taxRules: [],
  },
  {
    id: 'irs_schedule_c',
    name: 'Schedule C',
    description: 'Profit or Loss from Business',
    applicableEntityTypes: [EntityType.Business],
    taxRules: [],
  },
  {
    id: 'irs_form_1040',
    name: 'Form 1040',
    description: 'U.S. Individual Income Tax Return',
    applicableEntityTypes: [EntityType.Family, EntityType.Other],
    taxRules: [],
  },
];
