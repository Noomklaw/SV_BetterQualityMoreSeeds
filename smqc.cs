using Microsoft.Xna.Framework;
using StardewModdingAPI;
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
// Version 1.0 23.11.2018 mcoocr: initial release
// Version 1.1 23.11.2018 mcoocr: possible bugfix, game removes object during tick event handling causing NullReferenceException (using List with object reference instead of Dictionary)
//

namespace smcq
{
  public class ManagedSeedMaker
  {
    public StardewValley.Object RefSeedMaker;
    public StardewValley.Object RefDroppedObject;
    public bool HasBeenChecked;
    public bool IsDeprecated;

    public ManagedSeedMaker(StardewValley.Object seedMaker, StardewValley.Object droppedObject, bool isChecked)
    {
      RefSeedMaker = seedMaker;
      RefDroppedObject = droppedObject;
      HasBeenChecked = isChecked;
      IsDeprecated = false;
    }
  }

  public class Smcq : Mod
  {
    List<ManagedSeedMaker> _arSeedMakers;

    StardewValley.Object _previousHeldItem;
    GameLocation _previousLocation;
    bool _isInitialized;

    public override void Entry(IModHelper helper)
    {
        helper.Events.GameLoop.SaveLoaded += InitializeMod;
        helper.Events.GameLoop.ReturnedToTitle += ResetMod;
        helper.Events.GameLoop.UpdateTicked += ModUpdate;

      _arSeedMakers = new List<ManagedSeedMaker>();
      _arSeedMakers.Clear();

      _isInitialized = false;
    }

    private void InitializeMod(object sender, EventArgs e)
    {
      _previousLocation = Game1.player.currentLocation;
      _isInitialized = true;
    }

    private void ResetMod(object sender, EventArgs e)
    {
      _isInitialized = false;
    }

    private void ModUpdate(object sender, EventArgs e)
    {
      if (!_isInitialized)
        return;

      if ((Game1.player.currentLocation == null) || (Game1.player.currentLocation.Name == null))
        return;

      if ((_previousLocation.Name != Game1.player.currentLocation.Name)) // get Seed Makers in current location, save them to array
      {
        _arSeedMakers.Clear();

        foreach (Vector2 x in Game1.player.currentLocation.objects.Keys)
        {
          GameLocation gl = Game1.player.currentLocation;

          if (gl.objects[x] == null)
            continue;

          if (gl.objects[x].name.Equals("Seed Maker"))
          {
            //this.Monitor.Log($"existing (managed: {gl.objects[x].heldObject != null})");

            _arSeedMakers.Add(new ManagedSeedMaker(gl.objects[x], null, gl.objects[x].heldObject.Value != null));
          }
        }
      }
      else // load new placed Seed Makers  in current location into array
      {
        foreach (ManagedSeedMaker sm in _arSeedMakers)
        {
          sm.IsDeprecated = true;
        }

        foreach (Vector2 x in Game1.player.currentLocation.objects.Keys)
        {
          GameLocation gl = Game1.player.currentLocation;

          if (gl.objects[x] == null)
            continue;

          if (gl.objects[x].name.Equals("Seed Maker"))
          {
            if (_arSeedMakers.All(z => z.RefSeedMaker != gl.objects[x])) // found new, yet unmanaged Seed Maker
            {
              //this.Monitor.Log($"new (managed: {gl.objects[x].heldObject != null})");

              _arSeedMakers.Add(new ManagedSeedMaker(gl.objects[x], null, gl.objects[x].heldObject.Value != null));
            }
            else
            {
              _arSeedMakers.First(z => z.RefSeedMaker == gl.objects[x]).IsDeprecated = false;
            }
          }
        }

        // clear non-existent managed Seed Makers
        for(int x = 0; x < _arSeedMakers.Count; x++)
        {
          if(_arSeedMakers[x].IsDeprecated)
          {
            //this.Monitor.Log($"drop");
            _arSeedMakers.RemoveAt(x--);
          }
        }
      }

      _previousLocation = Game1.player.currentLocation;

      // check if an managed Seed Maker got crops inserted, if so add additional seeds based on crop quality
      foreach (ManagedSeedMaker msm in _arSeedMakers)
      {
        StardewValley.Object sm = msm.RefSeedMaker;

        if (msm.RefSeedMaker == null)
          continue;

        // new crop placed in managed Seed Maker
        if (sm.heldObject.Value != null && msm.HasBeenChecked == false && msm.RefDroppedObject == null)
        {
            //this.Monitor.Log($"trigger");

          msm.RefDroppedObject = _previousHeldItem;
          msm.HasBeenChecked = true;

          //this.Monitor.Log($"quality: {msm.refDroppedObject.quality}");

          var x = ((msm.RefDroppedObject.Quality == 4) ? (msm.RefDroppedObject.Quality - 1) : (msm.RefDroppedObject.Quality));

          //this.Monitor.Log($"stack: {sm.heldObject.Get().stack.Value}");
          //this.Monitor.Log($"add: {x}");

          sm.heldObject.Get().Stack = (sm.heldObject.Get().Stack + x);

          //this.Monitor.Log($"now: {sm.heldObject.Get().stack.Value}");
        }

        // seeds grabbed from Seed Maker, reset for new crops
        if (sm.heldObject.Value == null && msm.HasBeenChecked)
        {
          //this.Monitor.Log($"drop");

          msm.RefDroppedObject = null;
          msm.HasBeenChecked = false;
        }
      }

      _previousHeldItem = Game1.player.ActiveObject;
    }
  }
}
