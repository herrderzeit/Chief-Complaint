# Variant deck-builders: bag-builders, dice-builders, card-crafting, word builders and recent twists

Scope: mechanics these games add beyond the classic Dominion loop (buy from a market, shuffle, draw five, trash to thin). Each bullet is formatted as **Generic mechanic name**: explanation (games). Source. Rules details are paraphrased from reviews and rules pages. No review text is quoted at length.

## Card crafting, physical upgrading and cards that evolve

### Takeaway
Instead of adding cards, a deck can improve the cards it already has. The confirmed ways are layered sleeves (Mystic Vale), double-sided cards that flip to their upgraded side (Slay the Spire board game), interchangeable dice faces (Dice Forge) and threshold-based card power (Shards of Infinity). Fixed deck size with upgradeable slots is the cleanest version.

### Cited Findings
- **Layered card crafting (sleeve overlays)**: every card is a sleeve. Bought "advancement" transparencies slide into the sleeve on top of a base card, filling empty thirds. The deck stays at 20 sleeves all game, so the only way to grow is to improve the existing cards. (Mystic Vale) — [Opinionated Gamers](https://opinionatedgamers.com/2019/05/07/mystic-vale/); [GeekDad](https://geekdad.com/2016/07/mystic-vale/)
- **Fixed-size deck with upgrade slots**: because the sleeve count never changes, drawing worse cards over time is replaced by deciding which slot on which card to fill. Reviewers single this out as what makes the game feel fresh. (Mystic Vale) — [Nonsensical Gamers](https://www.nonsensicalgamers.com/?p=13891); [Opinionated Gamers](https://opinionatedgamers.com/2019/05/07/mystic-vale/)
- **Flip-to-upgrade double-sided cards**: each card prints its upgraded version on the back, so an upgrade is a flip rather than a swap. Reviewers call it clever and simple. (Slay the Spire: The Board Game, 2024) — [Cheddarface review](https://cheddarface.substack.com/p/board-game-review-slay-the-spire); [Meeple Mountain](https://meeplemountain.com/reviews/slay-the-spire-the-board-game)
- **Positional shop discount on a map node**: the merchant space has one slot marked as on sale, which costs 1 less. (Slay the Spire board game) — [Cheddarface review](https://cheddarface.substack.com/p/board-game-review-slay-the-spire)
- **Mastery threshold scaling**: a personal mastery track rises during play. Some cards, including the starter Shard Reactor and the Infinity Shard, gain stronger effects at set thresholds, and 30 or more mastery unlocks effectively unlimited damage. Cards therefore "level up" without being replaced. One critic argues this overshadows the deck-building. (Shards of Infinity, 2018) — [Pocket Gamer](https://www.pocketgamer.com/articles/080349/shards-of-infinity-review/); [Meeple Mountain](https://www.meeplemountain.com/reviews/shards-of-infinity/)
- **Interchangeable die faces (dice crafting)**: see the dice section below (Dice Forge). This is the dice equivalent of sleeve crafting.

### Inferences
- For a digital game, sleeve-style crafting maps naturally onto "card + modifier slots". A fixed-size deck (Mystic Vale) avoids dilution entirely and moves the decisions to *where* an upgrade goes.
- Flip-to-upgrade is the lightest-weight version: one boolean per card instance, kept as runtime state.

### Gaps
- Not verified this session: Mystic Vale's "Vale" cards (end-of-game scoring and passive bonus cards bought with spirit symbols). Also unverified: card-crafting peers such as Edge of Darkness, and sticker/legacy upgrades such as Clank! Legacy and Aeon's End Legacy, which were out of scope or not searched.
- No source retrieved on Ascension: Legends or Ascension: Tactics card evolution.

## Push-your-luck draws: draw until you bust, or stop

### Takeaway
Bag and deck variants replace "draw five" with "reveal one at a time until you choose to stop or a hazard threshold is crossed". The verified variants differ in what counts towards a bust (cumulative value, symbol count or blank dice), whether other cards can cancel hazards, and how harsh the bust penalty is. The penalty ranges from losing the whole turn to choosing between losing points or money, losing one action, or gaining a small consolation bonus.

### Cited Findings
- **Cumulative-hazard explosion (bag)**: ingredient chips are drawn one at a time from a bag. If the total value of the white "cherry bomb" chips drawn exceeds 7, the pot explodes and the player loses either that round's points or its money. (The Quacks of Quedlinburg, 2018) — [Board Game Co UK](https://board-game.co.uk/?p=226644); [Tabletop Gaming](https://tabletopgaming.co.uk/reviews/the-quacks-of-quedlinburg-review)
- **Catch-up allowance for trailing players**: Quacks has a well-regarded catch-up system, the "rat tails". It is mentioned in reviews, but its working was not confirmed this session (see Gaps). — [Meeple Mountain](https://www.meeplemountain.com/reviews/the-quacks-of-quedlinburg/)
- **Symbol-count spoil with a consolation token**: cards are flipped into play one by one. When too many decay symbols show (sources describe it as three visible plus one risky extra flip, which amounts to a fourth decay), the player "spoils", discards the field and loses the turn. Spoiling flips a token that gives +1 mana on the next turn that does not spoil. (Mystic Vale) — [Big Boss Battle](https://bigbossbattle.com/?p=38300); [GeekDad](https://geekdad.com/2016/07/mystic-vale/); [Play Board Games](https://www.play-board-games.com/?p=6629). *Note: the sources state the threshold differently (three plus one risky flip, versus "the fourth decay spoils"), but the two descriptions agree.*
- **Cancellable hazard symbols that cost actions**: animal cards are revealed one at a time. "Solitary" symbols count towards a bust and "gregarious" symbols cancel them. A net three solitary symbols ends drawing, and the player then takes only one action instead of two. (Living Forest, 2021) — [Tabletop Gaming](https://tabletopgaming.co.uk/Reviews/living-forest-review); [Board Game Co UK](https://board-game.co.uk/?p=2302276)
- **Blank-dice bust with a soft penalty (dice)**: players roll their dice, keep any that show symbols, and may reroll the rest. They bust only if three or more dice are active and a reroll shows all blanks. A bust gives a small consolation move on a separate fan track. (Cubitos, 2021) — [Board Game Quest](https://www.boardgamequest.com/cubitos-review/); [Meeple Mountain](https://www.meeplemountain.com/reviews/cubitos/)
- **Forced reveal (ink)**: spending ink draws an extra card face up, and that letter *must* be used in the next word; ink-drawn cards cannot be turned face down as wilds. This is the risk in a word deck-builder. (Hardback, 2018) — [Board Game Quest](https://www.boardgamequest.com/hardback-review/); [Casual Game Revolution](https://casualgamerevolution.com/node/1659)
- **Random-value stress cards**: a stress card has no fixed value. When it is played, cards are flipped from the deck until a speed card appears, and that card's value is used. (Heat: Pedal to the Metal, 2022) — [Gaming Trend](https://gamingtrend.com/reviews/heat-pedal-to-the-metal-review-formula-fun/); [Roll to Review](https://rolltoreview.com/?p=21354)

### Inferences
- In these designs the bust penalty is softened (Mystic Vale's mana token, Cubitos's fan track, Quacks's points-or-money choice), so busting still feels like progress. That is relevant to any "stability runs out" mechanic.
- Cancellable hazards (Living Forest) let deck-building actively manage the risk instead of only removing hazards.

### Gaps
- How Quacks' rat tails and the flask (taking back the last chip) work was not confirmed in retrieved sources.
- Not verified: shared bust tokens across players. No retrieved source showed a game with a shared bust pool.

## Deck as score, deck-thinning as the goal, and cards that leave permanently

### Takeaway
Several games make *removing* a card the scoring act, so the deck shrinks as players progress and keeping a strong card competes with banking it. In Valley of the Kings cards are entombed, in Tyrants cards are promoted, and in Dale they are built into a stall. Xenon Profiteer and Flamme Rouge turn deck reduction into the game's main arc. Undaunted turns battle losses into permanent card loss.

### Cited Findings
- **Entomb to score (removal equals scoring)**: only cards moved from the deck to a personal tomb score at game end. Normally one card is entombed per turn. Cards score by printed points or by unique sets, and a second copy adds nothing. The tension is between entombing a card and keeping it for its gold or action value. (Valley of the Kings, 2014) — [Opinionated Gamers](https://opinionatedgamers.com/2014/08/05/mary-dimercurio-prasad-review-of-valley-of-the-kings/); [Board Game Quest](https://boardgamequest.com/valley-of-the-kings-premium-edition-review); [Zatu](https://zatu.com/blogs/reviews/valley-of-the-kings-review)
- **Promote to an inner circle**: card effects move a card from hand into the "inner circle", where it is worth more victory points at the end but is removed from the deck. Reviewers describe this as trashing that scores and trace it to Valley of the Kings. (Tyrants of the Underdark, 2016) — [Miniature Market](https://miniaturemarket.com/reviewcorner/tyrants-of-the-underdark-review); [Space-Biff!](https://spacebiff.com/2016/09/13/tyrants-underdark/)
- **Stall-building race (cards leave the deck into fixed stacks)**: the goal is to build 8 stall stacks in sequence. Each stack is made from cards of one animal set at a set value, taken from hand, and the cards leave the deck permanently. Deck contents do not score themselves, and the race is to complete the stall. The review reports that bought cards go straight to hand and only played cards are discarded, so unplayed cards can be held for later stacks. (Dale of Merchants, 2015) — [Meeple Mountain](https://www.meeplemountain.com/reviews/dale-of-merchants/); [Miniature Market](https://miniaturemarket.com/reviewcorner/dale-of-merchants-review)
- **Deck deconstruction**: players start with a deck of mixed "air" element cards and shed oxygen, nitrogen and krypton to isolate xenon, which they distil to complete contracts. Removing cards is the core action. Air intake keeps adding cards, so a perfect deck is never reached. The game ends at a fifth completed contract or through an upgrade trigger. (Xenon Profiteer, 2015) — [Eagle Games publisher page](https://www.eaglegames.net/XENON-Profiteer-p/101656.htm); [The Thoughtful Gamer](https://thethoughtfulgamer.com/2022/03/09/xenon-profiteer-review/); [Opinionated Gamers](https://opinionatedgamers.com/2016/05/31/dale-yu-review-of-xenon-profiteer/)
- **Consumed-on-play deck (deck depletion)**: each rider has an energy deck. Players draw 4, play 1 and recycle the rest face up to the bottom of the deck. Played cards are removed, so the deck is used up over the race. (Flamme Rouge, 2016) — [Dized rules](https://rules.dized.com/flamme-rouge/rule/components/energy-and-exhaustion-cards); [Meeple Like Us](https://www.meeplelikeus.co.uk/flamme-rouge-2016/). *Conflict: one review says played cards are gone permanently, while another implies they are gone only for the current stage. This was not resolved from the rulebook.* — [Tabletop Together](https://tabletoptogether.com/2018/05/28/review-flamme-rouge/)
- **Casualties strip the deck**: when a unit is hit, the player loses a matching card from hand, deck or discard. Losses permanently shrink the deck, so voluntary thinning matters less. (Undaunted: Normandy, 2019) — [Kotaku](https://kotaku.com/undaunted-normandy-the-kotaku-review-1843721994); [Meeple Mountain](https://www.meeplemountain.com/reviews/undaunted-normandy-review-deck-building-on-d-day/)
- **Score-and-cull coupling**: each creature that survives a full round scores glory and also lets the player cull one used die. Scoring and thinning happen in the same act. (Quarriors, 2011) — [Opinionated Gamers](https://opinionatedgamers.com/2011/08/01/dale-yu-review-of-quarriors/); [Critical Boardgamer](https://criticalboardgamer.blogspot.com/2013/05/quarriors-review.html)
- **Single removal outlet**: in this bag-builder, removing followers depends largely on one building, the Town Hall, so thinning is a contested, deliberate investment. (Orléans, 2014) — [Zatu](https://zatu.com/blogs/reviews/orleans-review)

### Inferences
- "Bank it to score" (entomb or promote) maps closely onto hospital themes: discharging or curing a patient takes the card out of circulation in return for a lasting reward. For Chief Complaint that would be a patient card leaving the system as a scored "cured" record.
- Depletion decks (Flamme Rouge) and casualty decks (Undaunted) make the deck itself a stamina or health meter. That suits a shift-fatigue theme.

### Gaps
- Not verified: Tanto Cuore's "private quarters" (a similar scoring area). It was not searched.
- Xenon Profiteer's exact contract and auction rules are summarised from reviews, not the rulebook.

## Dice as cards, dice-building, dice crafting and token bags

### Takeaway
Dice-builders replace cards with dice drawn from a bag and rolled. The roll adds a second layer of randomness, so each "card" has several possible outcomes. Dice Forge goes further: it uses permanent dice and lets players physically swap the faces. Bag-builders (Orléans, Altiplano, Quacks) draw tokens rather than cards, and in some the tokens double as workers or resources.

### Cited Findings
- **Bag-drawn dice pool (dice-builder)**: dice are bought ("captured") from a shared supply into a personal bag, then drawn and rolled each turn. Each die is a set of possible outcomes rather than one fixed effect. (Quarriors, 2011) — [Opinionated Gamers](https://opinionatedgamers.com/2011/08/01/dale-yu-review-of-quarriors/)
- **Survival-gated scoring**: summoned creatures score only if they are still in play at the start of the owner's next turn, which gives opponents a reason to attack them. (Quarriors) — [Opinionated Gamers](https://opinionatedgamers.com/2011/08/01/dale-yu-review-of-quarriors/)
- **Knocked-out dice return in the next roll (prep area)**: draw 4 dice from the bag into a prep area each turn. A character die that is knocked out goes to its owner's prep area and is rolled again on the next turn as an extra die, so losing a unit is partly a gain. When the bag empties, the used pile goes back in. (Dice Masters, 2014 onwards) — [Rulespal rulebook](https://rulespal.com/marvel-dice-masters-uncanny-x-men/rulebook); [The Escapist](https://escapistmagazine.com/?p=58696)
- **Starter filler dice**: every player begins with identical weak sidekick dice, the counterpart of starter cards. (Dice Masters) — [Rulespal rulebook](https://rulespal.com/tmnt-dice-masters-heroes-in-a-half-shell/rulebook)
- **Interchangeable die faces (dice crafting)**: each player has two permanent dice with removable faces. Faces bought from a temple snap onto the dice, replacing weaker ones, so the "deck" is 12 faces that improve over time. (Dice Forge, 2017) — [Opinionated Gamers](https://opinionatedgamers.com/2017/08/11/dale-yu-review-of-dice-forge/); [Tabletop Gaming](https://tabletopgaming.co.uk/reviews/dice-forge-review)
- **Dice with different success odds and prices**: in Cubitos each die colour has a different spread of faces and a different cost, so a purchase buys a probability profile. (Cubitos) — [Opinionated Gamers](https://opinionatedgamers.com/2021/04/30/dale-yu-review-of-cubitos/); [Punchboard](https://punchboard.co.uk/?p=3356)
- **Dice manipulation (split, glue, nudge)**: not a pure dice-builder. Players spend scrapwood to raise a die's value, glue two dice into one with the combined value, or saw a die into smaller ones. (Woodcraft, 2022) — [Opinionated Gamers](https://opinionatedgamers.com/2023/08/17/dale-yu-another-look-at-woodcraft/); [Board Game Co UK](https://www.board-game.co.uk/woodcraft-review/)
- **Bag of follower tokens as workers**: followers (farmers, merchants and others) are drawn from a bag and assigned to action spaces. Recruiting adds to the bag. Reviewers describe this as bag-building with little contention over actions. (Orléans, 2014) — [RPG.net](https://www.rpg.net/reviews/archive/17/17079.phtml); [Zatu](https://zatu.com/blogs/reviews/orleans-review)
- **Resources that double as workers, with a discard pile**: Altiplano's tokens are both goods and action-givers. It adds a discard area that refills the bag only when the bag is empty, which makes the draw closer to a deck cycle. Tokens stored in a warehouse leave circulation. (Altiplano, 2017) — [Meeple Mountain](https://www.meeplemountain.com/reviews/altiplano-review-a-bag-building-sequel/); [Board Game Quest](https://boardgamequest.com/altiplano-review)
- **Chip bag with value thresholds**: see Quacks under push-your-luck.

### Inferences
- A token bag behaves like a deck with no hand-size cap and draws one at a time, so it suits per-token push-your-luck. A patient-arrival bag in Chief Complaint, with escalation chips that trigger events at a threshold, would follow the Quacks model.
- Knocked-out units returning as extra dice (Dice Masters) is a useful pattern for softening losses.

### Gaps
- Not verified: Dice Masters' team-construction limits and global abilities beyond rulebook basics.

## Novel markets, and cards that change hands between players

### Takeaway
Market twists here include decay discounts (Dale), limited copies per type (El Dorado), cards bought straight to hand or played on the spot (Dale, Shards of Infinity), and markets made of opponents' leftovers (Fort). In Fort a card you fail to play can be taken by an opponent.

### Cited Findings
- **Leftover-hand market (cards change hands)**: after playing, the active player drafts one new card, and any cards still in hand go face up to their "yard". Other players may draft from any yard as well as from the central offer. Cards a player hoards can therefore be lost to rivals. Best-friend cards are exempt and always go to the player's own discard pile. (Fort, Leder Games, 2020) — [Sprites & Dice](https://spritesanddice.com/reviews/reviewing-fort/); [Giant Brain](https://giantbrain.co.uk/2022/08/04/fort-review/)
- **Public versus private card abilities (copy by suit)**: in Fort a played card has a public ability that other players can copy by playing matching-suit cards, and a private ability for the owner. The suit match also chains further plays. (Fort) — [Sprites & Dice](https://spritesanddice.com/reviews/reviewing-fort/)
- **Hidden goal cards from progression**: reaching a fort level grants a choice of secret end-game scoring rules ("Made Up Rules"), and a later level grants public perks. (Fort) — [Sprites & Dice](https://spritesanddice.com/reviews/reviewing-fort/)
- **Sliding market discount**: market cards get cheaper the longer they sit, as they move down the row. (Dale of Merchants) — [Meeple Mountain](https://www.meeplemountain.com/reviews/dale-of-merchants/)
- **Buy straight to hand**: purchases go to hand rather than the discard pile, so they can be used or built into the stall the same turn. (Dale of Merchants, per the review) — [Meeple Mountain](https://www.meeplemountain.com/reviews/dale-of-merchants/)
- **Mercenary or one-shot buy**: some cards can be bought and played immediately, but they are then not kept in the deck. The choice is between gaining a card permanently and using it once now. (Shards of Infinity) — [Islay the Dragon](https://islaythedragon.com/featured/review-shards-of-infinity/)
- **Limited copies per card type (scarcity market)**: El Dorado's market has only 3 copies of each card type, so types run out. Bought cards go to the discard pile. The deck exists to move a piece along a race map, and cards are matched to terrain types. (The Quest for El Dorado, 2017; second edition 2023) — [Board Game Family](https://www.theboardgamefamily.com/2018/06/the-quest-for-el-dorado-board-game-review/); [Board Game Review UK](https://boardgamereview.co.uk/game-reviews/the-quest-for-el-dorado-2023-review)
- **Role selection adds the role card to your deck**: choosing a role (survey, warfare, colonise, produce or trade, research) performs it and adds that role card to the player's deck, so players improve at roles they repeat. Other players either follow with a weaker version or dissent and draw a card. The "market" is the set of role stacks. (Eminent Domain, 2011) — [Opinionated Gamers](https://opinionatedgamers.com/2011/10/10/review-of-eminent-domain-tasty-minstrel-games/); [The Thoughtful Gamer](https://thethoughtfulgamer.com/2018/08/27/eminent-domain-review/)
- **Negotiated co-op contracts**: players invite others onto their contract and negotiate how the rewards are split. Hazards drawn while on a contract cost prestige, and the first to 10 prestige wins. (Moonrakers, 2021) — [Gaming Trend](https://gamingtrend.com/reviews/moonrakers-dream-for-some-nightmare-for-others/); [Board Game Co UK](https://board-game.co.uk/moonrakers-board-game-review)
- **Any card as a wild (face down)**: in a word deck-builder any card can be played face down as a wild letter. A wild gives up the card's coins, prestige and ability, so the cost of flexibility is its value. Mystery-genre cards can turn adjacent wilds face up again. (Hardback, 2018) — [Board Game Quest](https://www.boardgamequest.com/hardback-review/); [Casual Game Revolution](https://casualgamerevolution.com/node/1659)
- **Genre or suit combo bonuses**: Hardback cards belong to genres (adventure, horror, mystery, romance). Using two or more cards of one genre in a word unlocks bonus effects, which rewards a focused deck. (Hardback) — [Board Game Quest](https://www.boardgamequest.com/hardback-review/)

### Inferences
- Fort's yard is a cheap, strong interaction lever: unplayed cards carry a cost without any attack cards. In a single-player hospital game, an equivalent would be patients or staff left untreated at the end of a shift becoming available to a rival or to a penalty pool.
- Eminent Domain's "the action you take becomes a card" suits a hospital deck-builder in which doing triage repeatedly makes you better at triage.

### Gaps
- Not verified this session: card pyramids or double-row markets, for example in Ascension or Star Realms: Frontiers. Also unverified: Moonrakers' crew recruiting rules and El Dorado's reserve-board rule, which retrieved reviews did not confirm.
- Not verified: Flip Ships and Xenoshyft market rules.

## Asymmetric starting decks, nation decks, and unrest or burden cards

### Takeaway
Asymmetry comes from per-faction decks that feed in gradually (Imperium) and per-player powers (Heart of Crown, Altiplano). Burden cards that clog the deck are a strong recurring motif: unrest in Imperium, heat in Heat, fog of war in Undaunted and exhaustion in Flamme Rouge. They come from overreaching, and removing them costs actions or tempo.

### Cited Findings
- **Nation deck that drip-feeds asymmetric cards**: each civilisation has a private nation deck. One card from it joins the draw deck every reshuffle. When it runs out, the accession card is added and the civilisation changes from barbarian to empire state. (Imperium: Classics and Imperium: Legends, 2021) — [Opinionated Gamers dual review](https://opinionatedgamers.com/2021/07/22/imperium-classic-and-imperium-legends-a-dual-review/); [Osprey blog](https://www.ospreypublishing.com/us/osprey-blog/2021/imperium-blog-mechanisms-overview/)
- **Faction-specific rule breaks**: some civilisations change the core loop. In Classics, for example, the Vikings' accession card is the last card of their deck and ends the game. (Imperium) — [Opinionated Gamers](https://opinionatedgamers.com/2021/07/22/imperium-classic-and-imperium-legends-a-dual-review/)
- **Unrest burden cards with a collapse end**: many gains bring unrest cards. They do nothing, cost points and are expensive to remove. Taking the last card from the shared unrest pile triggers "collapse", and the player with the least unrest wins outright. (Imperium) — [Tabletop Gaming](https://www.tabletopgaming.co.uk/Reviews/imperium-classics-review); [Osprey blog overview](https://www.ospreypublishing.com/uk/osprey-blog/2021/imperium-blog-game-overview/)
- **Self-inflicted burden from pushing (heat)**: going over the speed limit or using a boost adds heat cards. They carry no speed value and can only be cleared by cooling actions, or discarded in a lower gear or when in last place. (Heat: Pedal to the Metal, 2022) — [Gaming Trend](https://gamingtrend.com/reviews/heat-pedal-to-the-metal-review-formula-fun/); [Roll to Review](https://rolltoreview.com/?p=21354)
- **Exhaustion from positioning**: a rider with an open space ahead of it at the end of a round gains an exhaustion card worth 2 movement at the bottom of its deck. (Flamme Rouge) — [Dized rules](https://rules.dized.com/flamme-rouge/rule/components/energy-and-exhaustion-cards); [Miniature Market](https://www.miniaturemarket.com/reviewcorner/flamme-rouge-review)
- **Burden from exploring (fog of war)**: scouting new terrain adds fog-of-war cards that weaken hands. They are removed only by spending scout actions, which competes with other uses. (Undaunted: Normandy) — [ICv2](https://icv2.com/articles/reviews/view/43887/review-undaunted-normandy-game); [There Will Be Games](https://therewillbe.games/articles-boardgame-reviews/9057-undaunted-normandy-review)
- **Per-player leader powers**: players back one of several princesses, and each has an ability that changes certain actions. A reviewer notes that having to buy domain (territory) cards gives the game a somewhat scripted arc. (Heart of Crown, 2011; English edition 2017) — [Siliconera](https://www.siliconera.com/review-heart-of-crown-online-is-a-faithful-adaptation-of-the-deck-building-game/); [Board's Eye View](https://www.boardseyeview.net/post/2019/05/16/heart-of-crown)
- **Variable player powers in a bag-builder**: Altiplano adds asymmetric starting powers on top of the Orléans model. (Altiplano) — [Meeple Mountain](https://www.meeplemountain.com/reviews/altiplano-review-a-bag-building-sequel/)
- **Faction-tinted starting decks**: Fort gives each player a suit that matches their player board, plus a wild coin suit, so starting decks and combos differ slightly. (Fort) — [Sprites & Dice](https://spritesanddice.com/reviews/reviewing-fort/)

### Inferences
- Unrest, heat and fog of war are close analogues for hospital "burnout", "incident" or "complaint" cards: they are gained by overreaching (treating too many patients, cutting corners) and cleared only by spending a turn's action. Imperium's shared unrest pile that ends the game when empty is a ready-made run-ending pressure valve.
- Imperium's nation deck (one new card per reshuffle) is a good model for scripted escalation, such as an ED that gets busier over a shift without the player buying anything.

### Gaps
- Not verified this session (from memory only, so they need sourcing before use): the details of Heart of Crown's succession victory, where a backed princess plus succession points ends the game. Also unverified: Xenoshyft (a co-op tower defence deck-builder with a shared defence line); Paperback (the predecessor of Hardback, with vowel-based wilds and fame cards); Star Realms: Frontiers (co-op and boss challenges); Ascension: Legends and Ascension: Tactics; Flip Ships; Earthborne Rangers (whose deck also serves as fatigue and health); and Arcs, Cloudspire, Sky Islands, Witchstone, Dragon's Breath, Reavers of Midgard, Undergrove and Sagani. Several of these may not be deck-builders at all.
- No source found for any game using a *shared* bust-token pool across players.
