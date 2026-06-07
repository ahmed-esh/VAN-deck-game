using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VanGame.UI
{
  public class SouvenirVanItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
  {
    [SerializeField] string souvenirObjectName;

    public string SouvenirObjectName => souvenirObjectName;

    public event Action<SouvenirVanItem> Hovered;
    public event Action<SouvenirVanItem> Unhovered;

    public void Configure(string objectName)
    {
      souvenirObjectName = objectName;
      gameObject.name = objectName;
    }

    public void OnPointerEnter(PointerEventData eventData) => Hovered?.Invoke(this);

    public void OnPointerExit(PointerEventData eventData) => Unhovered?.Invoke(this);
  }
}
