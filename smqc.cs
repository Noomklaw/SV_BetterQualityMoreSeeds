using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

//
// smcq - Seed Maker Crop Quality
// Forked from "Better Quality More Seeds"
//
// Author: mcoocr
// Original Author: Space Baby
//

namespace smcq
{
  public class ManagedSeedMaker
  {
    public StardewValley.Object droppedObject;
    public bool hasBeenChecked;
    public bool isDeprecated;

    public ManagedSeedMaker()
    {
      droppedObject = null;
      hasBeenChecked = false;
      isDeprecated = false;
    }

    public ManagedSeedMaker(StardewValley.Object droppedObject, bool isChecked)
    {
      this.droppedObject = droppedObject;
      hasBeenChecked = isChecked;
      isDeprecated = false;
    }
  }

  public class Smcq : StardewModdingAPI.Mod
  {
    SerializableDictionary<StardewValley.Object, ManagedSeedMaker> allSeedMakers;

    StardewValley.Object previousHeldItem = null;
    GameLocation previousLocation = null;
    bool isInitialized = false;

    public override void Entry(IModHelper helper)
    {
      SaveEvents.AfterLoad += InitializeMod;
      SaveEvents.AfterReturnToTitle += ResetMod;
      GameEvents.UpdateTick += ModUpdate;

      allSeedMakers = new SerializableDictionary<StardewValley.Object, ManagedSeedMaker>();
      isInitialized = false;
    }

    private void InitializeMod(object sender, EventArgs e)
    {
      previousLocation = Game1.player.currentLocation;
      isInitialized = true;
    }

    private void ResetMod(object sender, EventArgs e)
    {
      isInitialized = false;
    }

    private void ModUpdate(object sender, EventArgs e)
    {
      List<StardewValley.Object> seedMakers = null;

      if (!isInitialized)
        return;

      if ((Game1.player.currentLocation == null) || (Game1.player.currentLocation.name == null))
        return;

      if ((previousLocation.name != Game1.player.currentLocation.name)) // lädt alle Seed Maker aus der neuen Location ins Array
      {
        allSeedMakers.Clear();

        foreach (Vector2 x in Game1.player.currentLocation.objects.Keys)
        {
          GameLocation gl = Game1.player.currentLocation;

          if (gl.objects[x] == null)
            continue;

          if (gl.objects[x].name.Equals("Seed Maker"))
          {
            //this.Monitor.Log($"existing (managed: {gl.objects[x].heldObject != null})");
            allSeedMakers.Add(gl.objects[x], new ManagedSeedMaker(null, gl.objects[x].heldObject != null ? true : false));
          }
        }
      }
      else // lädt alle neu platzierten Seed Maker ins Array und entfernt alte
      {
        List<StardewValley.Object> toRemove = new List<StardewValley.Object>();

        foreach (StardewValley.Object z in allSeedMakers.Keys)
        {
          allSeedMakers[z].isDeprecated = true;
        }

        foreach (Vector2 x in Game1.player.currentLocation.objects.Keys)
        {
          GameLocation gl = Game1.player.currentLocation;

          if (gl.objects[x] == null)
            continue;

          if (gl.objects[x].name.Equals("Seed Maker"))
          {
            bool found = false;

            foreach (StardewValley.Object z in allSeedMakers.Keys)
            {
              if (z == gl.objects[x])
              {
                allSeedMakers[z].isDeprecated = false;
                found = true;
                break;
              }
            }

            if (!found)
            {
              //this.Monitor.Log($"new (managed: {gl.objects[x].heldObject != null})");
              allSeedMakers.Add(gl.objects[x], new ManagedSeedMaker(null, gl.objects[x].heldObject != null ? true : false));
            }
          }
        }

        toRemove.Clear();

        foreach (StardewValley.Object z in allSeedMakers.Keys) // alte löschen
        {
          if (allSeedMakers[z].isDeprecated)
          {
            //this.Monitor.Log($"drop");
            toRemove.Add(z);
          }
        }

        foreach (StardewValley.Object z in toRemove)
        {
          allSeedMakers.Remove(z);
        }
      }

      previousLocation = Game1.player.currentLocation;
      seedMakers = allSeedMakers.Keys.ToList();

      // prüfen ob das Ausgabe-Inventar des Seed Maker angepasst werden muss
      if (seedMakers.Count > 0)
      {
        foreach (StardewValley.Object seedMaker in seedMakers)
        {
          if (seedMaker.heldObject.Value != null &&
            allSeedMakers[seedMaker].hasBeenChecked == false &&
            allSeedMakers[seedMaker].droppedObject == null) // Crop wurde im Seed Maker platziert
          {
            int x = 0;

            //this.Monitor.Log($"trigger");

            allSeedMakers[seedMaker].droppedObject = previousHeldItem;
            allSeedMakers[seedMaker].hasBeenChecked = true;

            //this.Monitor.Log($"quality: {allSeedMakers[seedMaker].droppedObject.quality}");

            x = ((allSeedMakers[seedMaker].droppedObject.quality == 4) ? (allSeedMakers[seedMaker].droppedObject.quality - 1) : (allSeedMakers[seedMaker].droppedObject.quality));

            //this.Monitor.Log($"stack: {seedMaker.heldObject.Get().stack.Value}");
            //this.Monitor.Log($"add: {x}");

            seedMaker.heldObject.Get().stack.Value = (seedMaker.heldObject.Get().stack.Value + x); // je nach Qualität Menge hinzuzählen

            //this.Monitor.Log($"now: {seedMaker.heldObject.Get().stack.Value}");
          }

          if (seedMaker.heldObject.Value == null &&
            allSeedMakers[seedMaker].hasBeenChecked == true) // Seeds wurden aus dem Seed Maker genommen
          {
            //this.Monitor.Log($"drop");

            allSeedMakers[seedMaker].droppedObject = null;
            allSeedMakers[seedMaker].hasBeenChecked = false;
          }
        }
      }

      previousHeldItem = Game1.player.ActiveObject;
    }
  }
}
