@ui
Feature: Asset lifecycle
  As an IT administrator
  I want new assets to be recorded accurately when they are checked out
  So that I always know who holds each device and what it cost

  Each run generates a unique asset tag, name, serial, order number and notes,
  so the scenario never clashes with other people using the shared demo.

  Background:
    Given I am logged in as an administrator

  Scenario: A new asset checked out to a user is listed with correct details and history
    When I create an asset checked out to a random user with these details:
      | Field           | Value           |
      | Model           | Macbook Pro 13" |
      | Status          | Ready to Deploy |
      | Purchase Date   | 2026-01-15      |
      | Purchase Cost   | 899.50          |
      | Warranty Months | 24              |
    Then the asset appears in the assets list with its name, serial and assignee
    When I open the asset from the assets list
    Then the asset page shows the details I entered
    And the asset is deployed to the selected user
    When I open the asset's history
    Then the history shows the asset was created
    And the history shows exactly one checkout to the selected user
