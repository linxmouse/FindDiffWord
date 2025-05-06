using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterClickHandler : MonoBehaviour, IPointerClickHandler
{
    private int characterIndex;

    public void Init(int index)
    {
        characterIndex = index;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        GameController.Instance.OnCharacterClicked(characterIndex);
    }
}
