const fs = require('fs');
const path = require('path');

const allowedItems = [
    // These are either already blacklisted by tag or they are stuff that shouldn't be blacklisted.
    "CLASSICITEMSRETURNS_ITEM_SNAKEEYES",
    "Duplicator",
    "ITEM_SANDSWEPT_THEIR_PROMINENCE",
    "ITEM_TWOSIDEDDIE",
    "ItemDefVoidJunkToScrapTier1",
    "MonstersOnShrineUse",
    "PowerCube",
    "PowerPyramid",
    "RegeneratingScrap",
    "RegeneratingScrapConsumed",
    "ScrapGreen",
    "ScrapGreenConsumed",
    "ScrapRed",
    "ScrapRedConsumed",
    "ScrapWhite",
    "ScrapWhiteConsumed",
    "ScrapGreenSuppressed",
    "ScrapRedSuppressed",
    "ScrapWhiteSuppressed",
    "ScrapYellow",
    "StatsFromScrap"
];

function analyzeFile(filePath) {
    const content = fs.readFileSync(filePath, 'utf8');
    const data = JSON.parse(content);

    // Read the CS file
    let csContent = '';
    try {
        csContent = fs.readFileSync(path.resolve(__dirname, '../src/PlayerDropTable.cs'), 'utf8');
    } catch (e) {
        console.warn('Could not read PlayerDropTable.cs:', e.message);
    }

    // Keywords for disabled interactables
    const keywords = [
        'chest', 'chests',
        'shrine', 'shrines',
        'printer', 'printers',
        'scrapper', 'scrappers',
        'duplicator', 'duplicators',
        'multishop', 'multishops',
        'terminal', 'terminals',
        'lockbox', 'lockboxes',
        'lock box', 'lock boxes'
    ];

    const results = [];

    function analyzeEntities(entities, type) {
        if (!entities) return;

        for (const entity of entities) {
            if (allowedItems.includes(entity.name)) {
                continue;
            } else if (csContent.includes(entity.name)) {
                continue;
            }

            let reasons = [];

            const searchTarget = (
                (entity.name || '') + ' ' +
                (entity.displayName || '') + ' ' +
                (entity.pickupDescription || '') + ' ' +
                (entity.fullDescription || '')
            ).toLowerCase();

            for (const kw of keywords) {
                const regex = new RegExp(`\\b${kw}\\b`, 'i');
                if (regex.test(searchTarget)) {
                    reasons.push(`Mentions "${kw}"`);
                }
            }

            // Check tags
            if (entity.tagsArray) {
                const hasScrapTag = entity.tagsArray.some(t => t.toLowerCase().includes('scrap'));
                if (hasScrapTag) {
                    reasons.push('Has "Scrap" related tag');
                }
            }
            // check for name keywords
            if (entity.name && entity.name.toLowerCase().includes('scrap')) {
                reasons.push('Name contains "scrap"');
            }
            // same for duplicator
            if (entity.name && entity.name.toLowerCase().includes('duplicator')) {
                reasons.push('Name contains "duplicator"');
            }
            if (entity.name && entity.name.toLowerCase().includes('lockbox')) {
                reasons.push('Name contains "lockbox"');
            }
            if (entity.name && entity.name.toLowerCase().includes('shrine')) {
                reasons.push('Name contains "shrine"');
            }
            // "gold key" in name -> lockbox
            if (entity.name && entity.name.toLowerCase().includes('gold') && entity.name.toLowerCase().includes('key')) {
                reasons.push('Name contains "gold" and "key" (lockbox)');
            }

            if (reasons.length > 0) {
                let cleanDesc = (entity.fullDescription || '').replace(/<[^>]*>/g, '').replace(/\n/g, ' ').trim();
                results.push({
                    type: type,
                    name: entity.name,
                    displayName: entity.displayName,
                    reasons: [...new Set(reasons)],
                    description: cleanDesc
                });
            }
        }
    }

    analyzeEntities(data.items, 'Item');
    analyzeEntities(data.equipments, 'Equipment');

    // Sort and display
    let mdOutput = `# Items/Equipments to Potentially Blacklist\n\n`;
    mdOutput += `| Type | Name | Display Name | Description | Reasons |\n`;
    mdOutput += `|---|---|---|---|---|\n`;

    for (const res of results) {
        mdOutput += `| ${res.type} | \`${res.name}\` | ${res.displayName} | ${res.description} | ${res.reasons.join('<br>')} |\n`;
    }

    fs.writeFileSync('analysis_result.md', mdOutput);
    console.log("Wrote to analysis_result.md");
}

try {
    analyzeFile('item_dump.json');
} catch (e) {
    console.error("Error analyzing file:", e.message);
}
