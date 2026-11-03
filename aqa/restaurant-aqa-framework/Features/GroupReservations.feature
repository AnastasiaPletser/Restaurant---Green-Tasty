Feature: Owner manages group pre-order

  @api @group-reservations @level1
  Scenario: Owner views group participants and order totals
    Given a customer has created a group reservation for 3 guests
    And two guests have joined the group
    When both guests submit the same dish with quantities 2 and 3
    And the owner views the group participants
    Then the group contains 3 guest sessions
    And each guest order total is displayed
    And the group total is correct

  @api @group-reservations @level2
  Scenario: Owner confirms a group pre-order with merged dishes
    Given a customer has created a group reservation for 3 guests
    And two guests have joined the group
    When both guests submit the same dish with quantities 2 and 3
    And the owner confirms the group pre-order
    Then the group pre-order contains one merged dish with quantity 5
    And the group pre-order has the submitted status

  @api @group-reservations @level3
  Scenario: Owner deletes a guest and updates group total
    Given a customer has created a group reservation for 3 guests
    And two guests have joined the group
    When both guests submit the same dish with quantities 2 and 3
    And the owner deletes one guest
    Then only 2 guest sessions remain in the group
    And the group total is recalculated