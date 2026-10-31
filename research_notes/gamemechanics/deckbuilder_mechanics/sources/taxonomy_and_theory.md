# Deck-Building Taxonomy, Design Theory and Healthcare-Themed Card Games

Research notes, 30 October 2026. Several primary pages could not be read directly: BoardGameGeek mechanic pages returned HTTP 403, the Dominion Strategy wiki sits behind an anti-bot check, and GDC Vault needs a login. In those cases the notes rely on search snippets or secondary summaries, and say so.

## 1. How do reference sites and Building Blocks of Tabletop Game Design classify deck-building and adjacent mechanics?

### Takeaway
BoardGameGeek files deck-building under one umbrella mechanic, "Deck, Bag, and Pool Building": acquire components into a personal pool, cycle through it, and improve it. It keeps acquisition modes (Open Drafting, Closed Drafting) and play-side mechanics (Hand Management) as separate tags. Engelstein & Shalev's encyclopedia puts deck-building inside a "Card Mechanisms" chapter, one of 13 categories. Design compendia treat deck-building as a child of "Engine Building", with Hand Management, Drafting and Set Collection as siblings.

### Cited Findings
- BGG's "Deck, Bag, and Pool Building" description (as quoted by a BGG blog post, because the mechanic page itself returned 403): players draw from individual decks, aim to acquire new cards, and cycle through their decks to improve them over time. The category explicitly includes bag building and pool building. The same post credits Dominion with pioneering the mechanism. — [BGG blogpost "Mechanic: Deck, Bag and Pool Building #3"](https://boardgamegeek.com/blogpost/117626/mechanic-deck-bag-and-pool-building-3)
- BGG "Open Drafting": players pick or purchase cards, tiles, resources or dice from a common pool to gain an advantage or build collections. It is distinguished from "Closed Drafting" (select and pass). — [BGG Open Drafting](https://boardgamegeek.com/boardgamemechanic/2041/open-drafting) (seen through a search snippet)
- BGG wiki "Hand Management": get the most value from the cards available in the current situation; playing cards in certain sequences or groups is rewarded. This wording is from the community wiki, not the official category page. — [BGG wiki: mechanism](https://boardgamegeek.com/wiki/page/mechanism)
- BGG's mechanic browse list carries all of these as separate tags. — [BGG Browse Mechanics](https://boardgamegeek.com/browse/boardgamemechanic)
- Wikipedia: a deck-building game is one where building a deck during play is central. Unlike CCGs, cards are not sold in random packs and most of the deck is built in play. Dominion (2008) "set the standard for the genre". StarCraft: The Board Game (2007) is sometimes cited as earlier, but that claim is flagged as needing a citation. Similar games without cards are called pool-building. Deck-building can be the core (Dominion, Star Realms) or one mechanic among several (Mage Knight, Arkham Horror LCG). In Legendary, players choose the market before the game. In digital games, "roguelike deck-builders" start with Dream Quest (2014) and took off with Slay the Spire (2017). — [Wikipedia: Deck-building game](https://en.wikipedia.org/wiki/Deck-building_game)
- Wikipedia separately defines pool building as building a pool of items, such as cards or dice, over the game. — [Wikipedia: Pool-building game](https://en.wikipedia.org/wiki/Pool-building_game) (through a search snippet)
- *Building Blocks of Tabletop Game Design: An Encyclopedia of Mechanisms* (Engelstein & Shalev; 1st ed. 2019, 2nd ed. 2022, 586 pp; a 3rd edition is listed for 2026) has 13 categories: Game Structure, Turn Order and Structure, Actions, Resolution, Game End and Victory, Uncertainty, Economics, Auctions, Worker Placement, Movement, Area Control, Set Collection, Card Mechanisms. Each category has 8 to 26 numbered mechanisms. — [Deane Barker review](https://deanebarker.net/library/titles/building-blocks-game-design/); [Routledge 3rd ed. listing](https://www.routledge.com/Building-Blocks-of-Tabletop-Game-Design-An-Encyclopedia-of-Mechanisms/Engelstein-Shalev/p/book/9781032985107); [British Library record](https://eld.bl.uk/catalog/020452378)
- Tallmove Game Mechanic Compendium: Deck-Building's parent is "Engine Building". Related mechanics are Hand Management, Drafting, Set Collection and Roguelike Meta-Progression. Its player-experience tags are engine satisfaction, planning, ownership, mastery, replayability and "runaway leader". Variants listed: direct conflict (Star Realms, Clank!), single-player roguelike builders (Dream Quest, Slay the Spire), and campaign builders where the deck persists (Pathfinder Adventure Card Game). Token or dice versions with the same structural logic: Orléans, The Quacks of Quedlinburg. — [Tallmove: Deck-Building](https://compendium.tallmove.com/mechanics/deck-building)

### Inferences
- A taxonomy that matches the references would have one parent, "Pool building (deck/bag/dice)", with these axes:
  - Acquisition: buy from a fixed supply, buy from a rotating row, draft or reward pick, gain or forced gain.
  - Cycle: draw, play, discard, reshuffle.
  - Deck manipulation: trash/thin, top-deck, filter, retain.
  - Play constraints: actions or energy, hand size (Hand Management).
  - Interaction: attacks, junking, market denial.
  - Clock and victory: pile-out, VP cards that clog, external threat track.
  This mirrors Engelstein & Shalev's split between Economics, Game End and Victory, and Card Mechanisms. The axis scheme is my own synthesis, not quoted from any one source.
- Chief Complaint's staff room market maps to "Open Drafting/Market". Its AP-limited play maps to Action Points plus Hand Management.

### Gaps
- I could not get the individual mechanism entries (names and numbers) inside Building Blocks' "Card Mechanisms" chapter, or its exact wording on deck-building. No preview or table of contents below chapter level was reachable. Building Blocks is a book, so verify any claim about its contents against a copy.
- I could not read the official BGG definition pages for Deck/Bag/Pool Building, Hand Management, Push Your Luck, Tableau Building or Market (403). Wording above is second-hand.

## 2. Design principles: Big Money versus engines, dilution, thinning, terminal collision, snowball, game-end triggers

### Takeaway
The Dominion community treats "Big Money" (buy only treasure and Victory cards) as the benchmark that any strategy must beat: about 4 Provinces by turn 17, or about 14 with one Smithy. Engines beat it only when they combine village, draw and payload, and they pay a set-up cost. The core tension of the genre is that every card dilutes every other card. Trashing is the counterweight, and supply-depletion end triggers plus VP cards that clog the deck limit snowballing.

### Cited Findings
- **Big Money benchmark.** The DS wiki calls Big Money a lower bound for advanced play, and in "very, very few" sets is BM (or BM plus one Action) dominant. In a game without attacks, BM averages 4 Provinces by about turn 17 and Smithy-BM by about turn 14. — [DS wiki: Money strategies](https://wiki.dominionstrategy.com/index.php/BMU) (through a search snippet; the page was bot-gated); [Guide to the Dominion Base Game](https://dominionstrategy.com/2011/06/19/guide-to-the-dominion-base-game/) (through a search snippet)
- Big Money is a deck that gets "the bulk of its income from treasures bigger than copper", has no support cards and cycles slowly. In the BM mirror "you have to green fast". Against engines, aim to reach 50% of the VP. The series names Big Money sub-types: Terminal Draw, Terminal Non-draw, Engine Hybrid, Treasure Flood. — [The Five Fundamental Deck Types: Big Money](https://dominionstrategy.com/2013/01/21/the-five-fundamental-deck-types-big-money/)
- **Money density and opportunity cost.** Weigh collision risk against the benefit of actions, and calculate with effective deck size, not actual size. — [The Keys to Big Money](https://dominionstrategy.com/2012/02/27/the-keys-to-big-money-money-density-and-opportunity-cost/) (through a search snippet)
- **Terminal collision** means drawing several terminal actions together when only one can be played. Estimated collision odds for a single terminal draw card of +1, +2 and +3 Cards are about 34%, 38% and 44%. — [DS glossary](https://dominionstrategy.com/glossary/); [DS wiki: Terminal](https://wiki.dominionstrategy.com/index.php/Terminal) (both through search snippets)
- A Rose-Hulman undergraduate maths paper finds that a second Smithy beats a Silver after turn 6, and a Gold after turn 8. — [Rose-Hulman Undergraduate Math Journal 25(1)](https://scholar.rose-hulman.edu/rhumj/vol25/iss1/2) (through a search snippet)
- **Engine anatomy:**
  - An engine is strong draw that reaches most or all of the deck in one turn, plus "payload".
  - Terminal draw needs villages (+2 Actions).
  - +Buy is needed to take several Provinces a turn.
  - Trashers are enablers, because stop cards such as Estates make drawing the deck fail.
  - Order of work: draw and villages first, then payload, then green once the deck draws itself.
  - Engines "can reliably beat Big Money" but take time to set up. Engines without payload lose to BM, and greening too early makes the engine sputter.
  — [Dominion 101: What is an Engine](https://dominionstrategy.com/2017/11/06/dominion-101-what-is-an-engine/)
- **Designer intent (Vaccarino's Secret History):**
  - Starting deck: originally 5 Copper/5 Estate, settled at 7 Copper/3 Estate. Starting cards are deliberately "junk", so bought cards become the stars.
  - Victory cards go in the deck so that your deck gets worse as you score, a built-in brake.
  - Turn baseline: one Action and one Buy, extended by cards.
  - End condition evolved: any empty pile, then any empty VP pile, then "Provinces or 3 piles", so a Duchy rush alone can't end it. Province went from 5 VP to 6.
  - Silver $3 and Gold $6 had "no special logic".
  - Random self-trashing attacks were cut because they could lock players out. Discard attacks were hard to balance (Militia "down to 3").
  - Kingdom of 10 randomised piles, with a recommended first-game set.
  — [The Secret History of Dominion](https://dominionstrategy.com/2013/06/24/the-secret-history-of-dominion/). Note: my fetch's summary also mentioned Alchemy and Duration lessons, which belong to later expansion Secret Histories. Treat those as unverified for this page.
- **Dilution, thinning and snowball (compendium view):**
  - Each added card lowers the frequency of all others, so indiscriminate buying weakens a deck.
  - Every purchase is "a delayed investment", because it only shows up after a reshuffle.
  - Most implementations include removal, which raises average card quality. In Slay the Spire, removal is a resource bought from merchants.
  - The feedback loop is mixed: better cards compound, while dilution damps them. A well-tuned early deck can still become "untouchable".
  - Counters: end triggers tied to supply depletion, which the leader can accelerate, and removal costs.
  - Draw order is input randomness. Players know the deck's composition and decide after seeing each hand, so perceived agency stays high.
  — [Tallmove: Deck-Building](https://compendium.tallmove.com/mechanics/deck-building)
- **Market structure:**
  - Fixed supply piles (Dominion) are easier to plan around.
  - A rotating face-up row (Ascension, Star Realms) adds variety and denial, but can lock a player out when everything is too expensive. Forum consensus is a few always-available fixed cards alongside a dynamic row.
  - Legendary-style pre-chosen markets are a third option.
  — [Tallmove](https://compendium.tallmove.com/mechanics/deck-building); [BGG thread: static or dynamic markets](https://boardgamegeek.com/thread/2480192/deckbuilders-static-or-dynamic-markets-which-do-yo) (through a search snippet; forum opinion); [Wikipedia](https://en.wikipedia.org/wiki/Deck-building_game)
- One commentator argues that games without thinning force players to dig through starting cards. — [Neutronium Games blog](https://neutronium.games/blog/deck-building-board-games-2026) (through a search snippet; opinion)
- **Digital balancing (Slay the Spire, GDC 2019, Anthony Giovannetti, "Metrics Driven Design and Balance"):** two key metrics were how often a card is picked when offered, and how often it appears in winning decks. A homemade tool logged every playtest run, alongside qualitative stream feedback. — [Game Developer GDC preview](https://www.gamedeveloper.com/design/learn-i-slay-the-spire-i-s-metrics-driven-approach-to-game-balancing-at-gdc-2019); [GDC Vault](https://www.gdcvault.com/browse/gdc-19/play/1025731) (login-gated)

### Inferences
- **"Mid-game shuffle maths."** A card bought this turn is first seen after the next reshuffle, so its expected first appearance is about one deck cycle away. Cycle length is deck size divided by cards drawn per turn. Late buys therefore have little value, which is why BM "greens" late and why there is a "penultimate"-type timing judgement. This is derived from the "delayed investment" point and the turn benchmarks above. I found no single citable article with the formula.
- Big Money is the natural benchmark bot for any deck-builder: a simple "never-buy" or "money-only" heuristic bot gives a lower bound for tuning. Chief Complaint's BalanceSim "never-buy bot" already plays this role.
- Vaccarino's "VP clogs the deck" brake has an obvious hospital analogue: complications or chronic-care burden cards entering the deck as you succeed. That would be a self-limiting snowball without explicit catch-up rules.
- Terminal-collision odds (34 to 44% for one terminal draw card) suggest that, under an AP-limited turn, single-use expensive cards will often collide. AP economy and "non-terminal" (AP-refunding) cards are the levers.

### Gaps
- I did not find a reliable dedicated article on catch-up mechanics, decision density or tempo specific to deck-builders. Those claims rely on the compendium and forum material.
- I did not retrieve the Penultimate Province Rule article (DS) or Board Game Design Lab notes. Both are commonly referenced but unverified here.
- The DS wiki pages (Engine, Collision, Thinning) were bot-blocked, so snippet-level only.
- The Slay the Spire talk's content on card removal or deck size could not be verified. The summary sites are AI-generated and were not used.

## 3. Healthcare-themed deck-builders and card games: which exist, and what are their mechanics?

### Takeaway
True medical deck-builders are rare in tabletop. The only clearly verified one is **Dynamite Nurse** (Arclight / Japanime Games, 2011), where hospitals cure wounded heroes and pass troublesome patients to rivals. Other medical games are cooperative action-point or worker-placement designs (Pandemic: Hot Zone, Healthy Heart Hospital, Dice Hospital, Hospital Rush, Doctor Panic) or take-that card games (Virus!), which offer adjacent mechanics rather than deck-building.

### Cited Findings
- **Dynamite Nurse** (Arclight / Japanime Games, 2011): BGG lists it as a deck-building card game. Hospitals cure wounded heroes and players pass troublesome patients to rivals. A reviewer notes more interaction than other Arclight deck-builders. There is a complaint that nurses can only be drawn, not searched for. A third-party blog says 3 to 5 players; patient deaths give "death mark" cards, and a "Dynamite Nurse" card goes to whoever holds the most. — [BGG: Dynamite Nurse](https://boardgamegeek.com/boardgame/108341/dynamite-nurse/ratings?comment=1); [The Geekly Grind](https://www.thegeeklygrind.com/all-posts/dynamite-nurse-board-game) (both through search snippets)
- **Pandemic: Hot Zone – North America** (Matt Leacock, Z-Man, 2020): a co-op for 2 to 4 players, cure three diseases. Mechanics: Action Points, Cooperative, Hand Management. Diseases can't be eradicated; optional Crisis cards raise difficulty. It plays in about half the time of Pandemic and is not a deck-builder. — [Board Game Oracle](https://www.boardgameoracle.com/en-AU/boardgame/price/dTUp3gP7FY/pandemic-hot-zone-north-america); [Z-Man announcement](https://www.zmangames.com/en/news/2020/5/7/hot-zone-series/); [TechRaptor review](https://techraptor.net/tabletop/reviews/pandemic-hot-zone-north-america-review)
- **Healthy Heart Hospital** (Victory Point Games; new edition later): a co-op economic game running a failing hospital over 9 rounds. Each round has Ambulance, Player Action and Housekeeping phases, and cube draws create heavy randomness. Mechanics: action points, co-op, push your luck, simulation, variable player powers. — [Armchair Dragoons review](https://armchairdragoons.com/classic-reviews-healthy-heart-hospital/); [Gameosity review](https://www.gameosity.com/2016/11/21/healthy-heart-hospital-review/); [Stately Play](https://statelyplay.com/2017/04/26/cardboard-critique-healthy-heart-hospital/) (through search snippets)
- **Hospital Rush**: competitive. Interns race to 10 prestige by placing two pawns a round on "fair" and "unfair" actions; unfair actions can be punished later by others. — [BGG: Hospital Rush](https://boardgamegeek.com/boardgame/119265/hospital-rush) (through a search snippet)
- **Doctor Panic**: a real-time co-op for 2 to 9 players, about 12 minutes, driven by a soundtrack. Overseers act as ER managers assigning tasks; the team must finish all treatments before the final heart attack. — [Board Game Rules: Doctor Panic](https://board-game-rules.com/boardgames/doctor-panic/) (through a search snippet)
- **Dice Hospital** (Alley Cat Games, 2018): hospital management in which patients are dice. Primarily worker placement, with one listing tagging it with deck/bag/pool building. — [Alley Cat Games: Dice Hospital](https://www.alleycatgames.com/dice-hospital) (through a search snippet; the tag is unverified on BGG)
- **Virus!** (Tranjis Games, 2015): 68 cards (organs, viruses, medicines, treatments).
  - Aim: collect four healthy organs of different colours.
  - Mechanics: hand management, set collection, take-that. Play one card or discard, then refill to a hand of 3.
  - One virus infects an organ and a second destroys it. One medicine protects and a second makes the organ immune.
  - Treatments include transplant, steal and contagion. Expansions: Virus! 2 Evolution (mutated viruses) and Halloween.
  — [UltraBoardGames rules](https://www.ultraboardgames.com/virus/game-rules.php); [Board Game Guys](https://boardgameguys.com/virus-2/)
- **Critical Care: The Game**: a co-op hospital/ICU game made by ICU doctors, with 250 cards, 45 to 90 minutes, ages 14+. I saw no evidence of deck-building. — [criticalcaregame.com](https://www.criticalcaregame.com/)
- **Hospital Life** (Focus Games): an educational board game on capacity management and patient flow, aimed at NHS training. — [Focus Games](https://shop.focusgames.com/products/hospital-life)
- **Card Doctor**: a digital roguelite deck-builder ("cosmic doctor"), in development with a target of March 2026. Current status unverified. — [itch.io: Card Doctor](https://jampactteam.itch.io/card-doctor-the-game)
- **Spotlight on Games list** of 108 medical titles, which identifies no deck-builders. Relevant entries:
  - CLINIC (Viard, 2014), a hospital-building game.
  - Friday Night at the ER (Gardner, 2007), a co-op patient-flow game used in healthcare training.
  - Acute Care (Norwood, 2015), a co-op card game about an ER shift.
  - Trauma Case Inbound (2007).
  - Triage (Dykes, 1980).
  - Healing Blade: Infectious Disease Card Battle (2010).
  - Malpractice! (2014).
  - Occam's Razor (2013).
  - Infected (Leacock, 2015).
  - Field Hospital (2010).
  - Royal Hospital.
  — [Spotlight on Games: Medicine](http://www.spotlightongames.com/list/medicine.html)
- **Making the Rounds: A Micro Nursing Game** exists on BGG. Its mechanics were not verified. — [BGG](https://boardgamegeek.com/boardgame/337358/making-the-rounds-a-micro-nursing-game)

### Inferences
- The market gap is real: apart from Dynamite Nurse, no published tabletop hospital deck-builder surfaced. Chief Complaint's niche (a medical deck-builder with patient triage) appears largely unoccupied.
- Mechanics worth borrowing from the medical neighbours:
  - Healthy Heart Hospital's arrivals phase ("Ambulance") and push-your-luck: compare Chief Complaint's existing Ambulance handover.
  - Virus!'s two-step infect/destroy and protect/immunise state machine, a clean model for symptom escalation versus stabilisation.
  - Dynamite Nurse's "pass the troublesome patient" and death-mark penalty cards: the Vaccarino-style deck clog applied to failure.
  - Pandemic's escalating outbreak clock.
  - Doctor Panic's real-time triage manager role.

### Gaps
- I could not verify the existence or mechanics of "Viral" (2017), "Pandemic: The Cure", "Plague Inc: The Board Game", "Clinic: Deluxe Edition" details, "Med School", "Nurse!", "The Plague" or "Surgeon Simulator" card games within the tool budget. Treat any claims about them as unverified.
- Dynamite Nurse's full rules (market structure, starting deck) were not retrieved, only BGG and blog snippets.
- Most BGG pages (403) could not be read directly. Mechanic tags quoted here come from aggregators (Board Game Oracle) or snippets.
