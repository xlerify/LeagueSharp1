﻿using System;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

namespace Master
{
    class Udyr : Program
    {
        private const String Version = "1.0.0";
        private enum Stance
        {
            Tiger,
            Turtle,
            Bear,
            Phoenix
        }
        private Stance CurStance;
        private Int32 AACount = 0;
        private bool TigerActive = false, PhoenixActive = false;
        private Obj_AI_Base minionObj;

        public Udyr()
        {
            SkillQ = new Spell(SpellSlot.Q, 600);
            SkillW = new Spell(SpellSlot.W, 600);
            SkillE = new Spell(SpellSlot.E, 600);
            SkillR = new Spell(SpellSlot.R, 600);

            Config.AddSubMenu(new Menu("Key Bindings", "KeyBindings"));
            Config.SubMenu("KeyBindings").AddItem(new MenuItem(Name + "stunActive", "Stun Cycle").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("KeyBindings").AddItem(new MenuItem(Name + "fleeActive", "Flee").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Combo/Harass", "csettings"));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "qusage", "Use Q").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "wusage", "Use W").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "eusage", "Use E").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "rusage", "Use R").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "ignite", "Auto Ignite If Killable").SetValue(true));
            Config.SubMenu("csettings").AddItem(new MenuItem(Name + "iusage", "Use Item").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "miscs"));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "useAntiE", "Use E To Anti Gap Closer").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem(Name + "useInterE", "Use E To Interrupt").SetValue(true));

            Config.AddSubMenu(new Menu("Lane/Jungle Clear", "LaneJungClear"));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearQ", "Use Q").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearW", "Use W").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearE", "Use E").SetValue(true));
            Config.SubMenu("LaneJungClear").AddItem(new MenuItem(Name + "useClearR", "Use R").SetValue(true));

            Game.OnGameUpdate += OnGameUpdate;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Obj_AI_Base.OnCreate += OnCreate;
            Obj_AI_Base.OnDelete += OnDelete;
            Game.PrintChat("<font color = \"#33CCCC\">Master of {0}</font> <font color = \"#fff8e7\">Brian v{1}</font>", Name, Version);
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;
            var target = SimpleTs.GetTarget(1500, (CurStance == Stance.Tiger) ? SimpleTs.DamageType.Physical : SimpleTs.DamageType.Magical);
            if (Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Mixed && targetObj != null)
            {
                targetObj = null;
            }
            else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                targetObj = target;
            }
            else if ((target.IsValidTarget() && targetObj == null) || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                targetObj = target;
            }
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                NormalCombo();
            }
            else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear) LaneJungClear();
            if (Config.Item(Name + "stunActive").GetValue<KeyBind>().Active) StunCycle();
            if (Config.Item(Name + "fleeActive").GetValue<KeyBind>().Active) Flee();
        }

        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!Config.Item(Name + "useAntiE").GetValue<bool>()) return;
            if (gapcloser.Sender.IsValidTarget(SkillE.Range) && !gapcloser.Sender.HasBuff("udyrbearstuncheck", true) && (SkillE.IsReady() || CurStance == Stance.Bear))
            {
                if (CurStance != Stance.Bear) SkillE.Cast();
                Player.IssueOrder(GameObjectOrder.AttackUnit, gapcloser.Sender);
            }
        }

        private void OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!Config.Item(Name + "useInterE").GetValue<bool>()) return;
            if (unit.IsValidTarget(SkillE.Range) && !unit.HasBuff("udyrbearstuncheck", true) && (SkillE.IsReady() || CurStance == Stance.Bear))
            {
                if (CurStance != Stance.Bear) SkillE.Cast();
                Player.IssueOrder(GameObjectOrder.AttackUnit, unit);
            }
        }

        void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            if (args.SData.Name == SkillQ.Instance.Name)
            {
                CurStance = Stance.Tiger;
                AACount = 0;
            }
            else if (args.SData.Name == SkillW.Instance.Name)
            {
                CurStance = Stance.Turtle;
                AACount = 0;
            }
            else if (args.SData.Name == SkillE.Instance.Name)
            {
                CurStance = Stance.Bear;
                AACount = 0;
            }
            else if (args.SData.Name == SkillR.Instance.Name)
            {
                CurStance = Stance.Phoenix;
                AACount = 0;
            }
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                if (args.SData.Name == Name + "BasicAttack" && (args.Target == targetObj || args.Target == minionObj) && (CurStance == Stance.Tiger || CurStance == Stance.Phoenix)) AACount += 1;
            }
        }

        void OnCreate(GameObject sender, EventArgs args)
        {
            if (Player.Distance(sender.Position) <= 70 && (sender.Name == "Udyr_PhoenixBreath_cas.troy" || sender.Name == "Udyr_Spirit_Phoenix_Breath_cas.troy")) PhoenixActive = true;
            if (Player.Distance(sender.Position) <= 450 && (sender.Name == "udyr_tiger_claw_tar.troy" || sender.Name == "Udyr_Spirit_Tiger_Claw_tar.troy")) TigerActive = true;
        }

        void OnDelete(GameObject sender, EventArgs args)
        {
            if (Player.Distance(sender.Position) <= 70 && (sender.Name == "Udyr_PhoenixBreath_cas.troy" || sender.Name == "Udyr_Spirit_Phoenix_Breath_cas.troy")) PhoenixActive = false;
            if (Player.Distance(sender.Position) <= 450 && (sender.Name == "udyr_tiger_claw_tar.troy" || sender.Name == "Udyr_Spirit_Tiger_Claw_tar.troy")) TigerActive = false;
        }

        private void NormalCombo()
        {
            if (targetObj == null) return;
            if (Config.Item(Name + "eusage").GetValue<bool>() && SkillE.IsReady() && !targetObj.HasBuff("udyrbearstuncheck", true)) SkillE.Cast();
            if (targetObj.IsValidTarget(400) && (!Config.Item(Name + "eusage").GetValue<bool>() || (Config.Item(Name + "eusage").GetValue<bool>() && (SkillE.Level == 0 || (SkillE.Level >= 1 && targetObj.HasBuff("udyrbearstuncheck", true))))))
            {
                if (Config.Item(Name + "qusage").GetValue<bool>() && SkillQ.IsReady()) SkillQ.Cast();
                if (Config.Item(Name + "rusage").GetValue<bool>() && SkillR.IsReady() && SkillQ.Level >= 1 && CurStance == Stance.Tiger && (AACount >= 2 || TigerActive))
                {
                    SkillR.Cast();
                }
                else if (SkillQ.Level == 0 && SkillR.IsReady()) SkillR.Cast();
                if (Config.Item(Name + "wusage").GetValue<bool>() && SkillW.IsReady())
                {
                    if (CurStance == Stance.Phoenix && (AACount >= 3 || PhoenixActive))
                    {
                        SkillW.Cast();
                    }
                    else if (CurStance == Stance.Tiger && (AACount >= 2 || TigerActive))
                    {
                        SkillW.Cast();
                    }
                    else if (SkillQ.Level == 0 && SkillR.Level == 0) SkillW.Cast();
                }
            }
            if (Config.Item(Name + "iusage").GetValue<bool>()) UseItem(targetObj);
            if (Config.Item(Name + "ignite").GetValue<bool>()) CastIgnite(targetObj);
        }

        private void LaneJungClear()
        {
            minionObj = MinionManager.GetMinions(Player.Position, SkillE.Range, MinionTypes.All, MinionTeam.NotAlly).FirstOrDefault();
            if (minionObj == null) return;
            Orbwalker.ForceTarget(minionObj);
            if (Config.Item(Name + "useClearE").GetValue<bool>() && SkillE.IsReady() && !minionObj.HasBuff("udyrbearstuncheck", true)) SkillE.Cast();
            if (minionObj.IsValidTarget(400) && (!Config.Item(Name + "useClearE").GetValue<bool>() || (Config.Item(Name + "useClearE").GetValue<bool>() && (SkillE.Level == 0 || (SkillE.Level >= 1 && minionObj.HasBuff("udyrbearstuncheck", true))))))
            {
                if (Config.Item(Name + "useClearQ").GetValue<bool>() && SkillQ.IsReady()) SkillQ.Cast();
                if (Config.Item(Name + "useClearR").GetValue<bool>() && SkillR.IsReady() && SkillQ.Level >= 1 && CurStance == Stance.Tiger && (AACount >= 2 || TigerActive))
                {
                    SkillR.Cast();
                }
                else if (SkillQ.Level == 0 && SkillR.IsReady()) SkillR.Cast();
                if (Config.Item(Name + "useClearW").GetValue<bool>() && SkillW.IsReady())
                {
                    if (CurStance == Stance.Phoenix && (AACount >= 3 || PhoenixActive))
                    {
                        SkillW.Cast();
                    }
                    else if (CurStance == Stance.Tiger && (AACount >= 2 || TigerActive))
                    {
                        SkillW.Cast();
                    }
                    else if (SkillQ.Level == 0 && SkillR.Level == 0) SkillW.Cast();
                }
            }
        }

        private void StunCycle()
        {
            Orbwalk();
            Obj_AI_Hero targetClosest = ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget(SkillE.Range) && !i.HasBuff("udyrbearstuncheck", true)).OrderBy(i => i.Distance(Player)).FirstOrDefault();
            if (targetClosest == null) return;
            if (SkillE.IsReady()) SkillE.Cast();
            Player.IssueOrder(GameObjectOrder.AttackUnit, targetClosest);
        }

        private void Flee()
        {
            var manaQ = SkillQ.Instance.ManaCost;
            var manaW = SkillW.Instance.ManaCost;
            var manaR = SkillR.Instance.ManaCost;
            var PData = Player.Buffs.FirstOrDefault(i => i.Name == "udyrmonkeyagilitybuff");
            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            if (SkillE.IsReady()) SkillE.Cast();
            if (PData != null && PData.Count < 3)
            {
                if ((manaQ < manaW || manaQ < manaR || (manaQ == manaW && manaQ < manaR) || (manaQ == manaR && manaQ < manaW)) && SkillQ.IsReady())
                {
                    SkillQ.Cast();
                }
                else if ((manaW < manaQ || manaW < manaR || (manaW == manaQ && manaW < manaR) || (manaW == manaR && manaW < manaQ)) && SkillW.IsReady())
                {
                    SkillW.Cast();
                }
                else if ((manaR < manaQ || manaR < manaW || (manaR == manaQ && manaR < manaW) || (manaR == manaW && manaR < manaQ)) && SkillR.IsReady()) SkillR.Cast();
            }
        }

        private void UseItem(Obj_AI_Hero target)
        {
            if (Items.CanUseItem(Bilge) && Player.Distance(target) <= 450) Items.UseItem(Bilge, target);
            if (Items.CanUseItem(Blade) && Player.Distance(target) <= 450) Items.UseItem(Blade, target);
            if (Items.CanUseItem(Rand) && Utility.CountEnemysInRange(450) >= 1) Items.UseItem(Rand);
        }
    }
}