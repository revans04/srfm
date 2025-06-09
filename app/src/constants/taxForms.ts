// src/constants/taxForms.ts
import { EntityType, TaxForm } from "../types";

export const DEFAULT_TAX_FORMS: TaxForm[] = [
  {
    id: "form_1040",
    name: "Form 1040",
    description: "U.S. Individual Income Tax Return",
    applicableEntityTypes: [EntityType.Family, EntityType.Other],
    taxRules: ["rule_personal_income", "rule_meals", "rule_mileage"],
  },
  {
    id: "schedule_c",
    name: "Schedule C",
    description: "Profit or Loss from Business (Sole Proprietorship)",
    applicableEntityTypes: [EntityType.Business],
    taxRules: [
      "rule_cogs",
      "rule_business_marketing",
      "rule_platform_fees",
      "rule_payment_processing",
      "rule_saas_fees",
      "rule_shipping",
      "rule_business_taxes",
      "rule_capital_expenses",
      "rule_meals",
      "rule_mileage",
      "rule_office_supplies",
    ],
  },
  {
    id: "schedule_e",
    name: "Schedule E",
    description: "Supplemental Income and Loss (Rental Properties)",
    applicableEntityTypes: [EntityType.RentalProperty],
    taxRules: [
      "rule_rental_repairs",
      "rule_rental_taxes",
      "rule_rental_insurance",
      "rule_rental_vacancy",
      "rule_rental_utilities",
      "rule_rental_landscaping",
      "rule_rental_pest_control",
      "rule_rental_capital_expenses",
    ],
  },
];