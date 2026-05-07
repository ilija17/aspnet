# Semantic DB Model – Casino App

## Tables / Classes

### Casino
| Property | Type | Notes |
|---|---|---|
| Id | int | PK |
| Name | string | Casino name |
| Address | string | Street address |
| LicenseNumber | string | Regulatory license |
| FoundedDate | DateTime | Opening date |

### Game
| Property | Type | Notes |
|---|---|---|
| Id | int | PK |
| Name | string | Game name |
| Type | GameType (enum) | Slots, Poker, Blackjack, Roulette |
| MinBet | decimal | Minimum allowed bet |
| MaxBet | decimal | Maximum allowed bet |
| Description | string | Short description |

### Table
| Property | Type | Notes |
|---|---|---|
| Id | int | PK |
| TableNumber | int | Number within casino |
| IsAvailable | bool | Live availability flag |
| MinBet | decimal | Table-specific min bet |
| MaxBet | decimal | Table-specific max bet |
| CasinoId | int | FK → Casino |
| GameId | int | FK → Game |

### Player
| Property | Type | Notes |
|---|---|---|
| Id | int | PK |
| FirstName | string | |
| LastName | string | |
| Email | string | Unique contact |
| DateOfBirth | DateTime | Age verification |
| Balance | decimal | Current wallet balance |

### Employee
| Property | Type | Notes |
|---|---|---|
| Id | int | PK |
| FirstName | string | |
| LastName | string | |
| Position | string | Dealer / Manager / Security / Cashier |
| CasinoId | int | FK → Casino |

### Reservation
| Property | Type | Notes |
|---|---|---|
| Id | int | PK |
| ReservedAt | DateTime | Reservation timestamp |
| PlayerId | int | FK → Player |
| TableId | int | FK → Table |

### Transaction
| Property | Type | Notes |
|---|---|---|
| Id | int | PK |
| Amount | decimal | Transaction value |
| Type | TransactionType (enum) | Deposit, Withdrawal, Bet, Win |
| CreatedAt | DateTime | When it occurred |
| PlayerId | int | FK → Player |

## Relationships

| From | To | Type | Description |
|---|---|---|---|
| Casino | Table | 1-N | One casino hosts many tables |
| Casino | Employee | 1-N | One casino employs many staff |
| Game | Table | 1-N | One game type runs on many tables |
| Player | Transaction | 1-N | One player has many transactions |
| Player | Reservation | 1-N | One player holds many reservations |
| Table | Reservation | 1-N | One table can have many reservations |
| Player ↔ Table | Reservation (join) | N-N | Reservations connect players to tables |

## Enums

**GameType**: `Slots`, `Poker`, `Blackjack`, `Roulette`

**TransactionType**: `Deposit`, `Withdrawal`, `Bet`, `Win`
