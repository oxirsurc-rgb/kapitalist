using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DemocracySim.Engine.Core;
using DemocracySim.Engine.World;
using DemocracySim.Engine.Legislative;

// R-REFACTOR: UIManager artık bir "kabuk": tuval, sekmeler, modal ve toast yönetimi.
// Sayfa içerikleri ilgili Presenter sınıflarında kurulur. Davranış değişmedi.

internal abstract class PresenterBase
{
    protected readonly UIManager ui;
    protected PresenterBase(UIManager ui) { this.ui = ui; }
}

/// <summary>Sekme sayfası kuran presenter.</summary>
internal abstract class PagePresenter : PresenterBase
{
    protected PagePresenter(UIManager ui) : base(ui) { }
    public abstract void Build(RectTransform c, SimulationEngine e);
}
