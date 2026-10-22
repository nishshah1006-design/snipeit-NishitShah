@ui
Feature: Asset validation
  As an IT administrator
  I want Snipe-IT to reject invalid asset data
  So that every asset can be identified unambiguously

  Background:
    Given I am logged in as an administrator

  Scenario: An asset tag that is already in use is rejected
    Given an asset already exists with these details:
      | Field  | Value           |
      | Model  | Macbook Pro 13" |
      | Status | Ready to Deploy |
    When I try to create another asset with the same asset tag
    Then the asset tag is rejected with the error "The Asset Tag must be unique."
