using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace MagicStardew
{
    
    public class ModConfig
    {
        public enum UnlockMode { Legacy, Ashara }
        public UnlockMode SpellUnlockMode { get; set; } = UnlockMode.Ashara;
        public bool DevMode { get; set; } = false;
        [Obsolete("Use AutoManaBarPosition instead")]
        public int ManaBarPosition { get; set; } = 1;

        public bool AutoManaBarPosition { get; set; } = true; // dawniej ManaBarPosition jako bool
        public int CustomManaBarX { get; set; } = 20;
        public int CustomManaBarY { get; set; } = 100;

        public string ManaBarRecolor { get; set; } = "Auto";
        public string SpellBookRecolor { get; set; } = "Auto";
        public int MaxManaCap { get; set; } = 450; // domyślnie 450

        public bool CustomManaRegen { get; set; } = false;
        public int ManaRegenPercent { get; set; } = 100;

        public KeybindList OpenSpellBookKey { get; set; } = new(SButton.K);

        public int MenuOffsetX { get; set; } = 0;
        public int MenuOffsetY { get; set; } = 0;

        public float ScaleFactor { get; set; } = 0.65f;

        public bool UseRandomFlowerRow { get; set; } = true;
        public int FlowerRowIndex { get; set; } = 0;
        public bool EnableFlowerPlanting { get; set; } = true;
        public string VictorianManaBar { get; set; } = "JetBlack";
        public string VictorianSpellBook { get; set; } = "JetBlack";
        public string BirdColor2 { get; set; } = "Random";

        public string SpellBookColor { get; set; } = "Purple";

        public bool StumpsAndBolder { get; set; } = true;

        public string CatTexture { get; set; } = "Greay_Cat.png"; // domyślna tekstura
        public bool EnableMeowSound { get; set; } = true;
        public Dictionary<int, int> FavoriteSpells { get; set; } = new();

        public bool FlashOn { get; set; } = true;
        public KeybindList[] FavoriteSpellHotkeys { get; set; } =
        {
            new KeybindList(new Keybind(SButton.LeftShift, SButton.D1)),
            new KeybindList(new Keybind(SButton.LeftShift, SButton.D2)),
            new KeybindList(new Keybind(SButton.LeftShift, SButton.D3)),
            new KeybindList(new Keybind(SButton.LeftShift, SButton.D4)),
            new KeybindList(new Keybind(SButton.LeftShift, SButton.D5))
        };

        public KeybindList CleanseHotkey { get; set; } = new KeybindList(
        new Keybind(SButton.LeftShift, SButton.M)


        );
        public int SpellBookShopPrice { get; set; } = 0;


        //text color spell menu
        public string SpellNameColor { get; set; } = "#B464FF";   // domyślnie fiolet
        public string SpellDescColor { get; set; } = "#FFFFFF";   // biały
        public string ManaLabelColor { get; set; } = "#6495ED";   // cornflowerblue
        public string ManaValueColor { get; set; } = "#FFFFFF";   // biały
        public string UnlockHintColor { get; set; } = "#D3D3D3";  // jasnoszary
        public bool DynamicManaBarSize { get; set; } = false;
        public int ExplosiveHammerRadius { get; set; } = 1; // domyślnie 1

        //spells register
        public bool EnableHaste { get; set; } = true;
        public bool EnableFireCyclone { get; set; } = true;
        public bool EnableFarmWarp { get; set; } = true;
        public bool EnableStormDance { get; set; } = true;
        public bool EnableWeatherWonders { get; set; } = true;
        public bool EnableWaterCloud { get; set; } = true;
        public bool EnablePlayerWarp { get; set; } = true;
        public bool EnableTimeRewind { get; set; } = true;
        public bool EnableGrow { get; set; } = true;
        public bool EnableCharisma { get; set; } = true;
        public bool EnableEarthquake { get; set; } = true;
        public bool EnableFlowerStep { get; set; } = true;
        public bool EnableLumberSpirit { get; set; } = true;
        public bool EnableStoneSpirit { get; set; } = true;
        public bool EnableHeal { get; set; } = true;
        public bool EnableMagneticSkin { get; set; } = true;
        public bool EnableBirdFriend { get; set; } = true;
        public bool EnableInstantBite { get; set; } = true;
        public bool EnableMachineSpeedUp { get; set; } = true;
        public bool EnablePoisonsword { get; set; } = true;
        public bool Enablefirefly { get; set; } = true;
        public bool Enableanimallovery { get; set; } = true;
        public bool EnableRandomOutfitSpell { get; set; } = true;
        public bool EnableManaBall { get; set; } = true;
        public bool EnableChefSenseSpell { get; set; } = true;
        public bool EnableCallHorseSpell { get; set; } = true;

        public bool EnableExplosiveHammer { get; set; } = true;
        public bool EnableCleanseSpell { get; set; } = true;
        public int CleanseManaCost { get; set; } = 0;
        public bool EnableMonsterMuskSpell { get; set; } = true;
        public bool EnableIslandWarpSpell { get; set; } = true;
        public bool EnableLittleCuteFlamethrowerSpell { get; set; } = true;
        public bool EnableSeasBountySpell { get; set; } = true;
        public bool EnableLivingChestSpell { get; set; } = true;
        public bool EnableTimberfallSpell { get; set; } = true;
        public bool EnableBerserkSpell { get; set; } = true;

        public bool EnablePathfinderMarkSpell { get; set; } = true;

        public bool EnablFieldOfFlowersSpell { get; set; } = true;
        public bool EnableMyceliumSpell { get; set; } = true;
        public bool EnableHarvestSpiritSpell { get; set; } = true;
        public bool EnableFireSlashSpell { get; set; } = true;
        public bool EnableCrabSkinSpell { get; set; } = true;
        public bool EnablePhaseWalkSpell { get; set; } = true;
        public bool EnableNewVegasSpell { get; set; } = true;
        public bool EnableVoidBinSpell { get; set; } = true;
        public bool EnableRefreshSpell { get; set; } = true;
        public bool EnableGrassSpell { get; set; } = true;
        public bool EnableSeekersEyeSpell { get; set; } = true;
        public bool EnableMidasPulseSpell { get; set; } = true;
        public bool EnableMachineHarvest { get; set; } = true;
        public bool EnableShineSpell { get; set; } = true;
        public bool EnableScuffSpell { get; set; } = true;
        public bool EnableBountifulHarvest { get; set; } = true;
    }
}
