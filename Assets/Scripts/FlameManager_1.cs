using UnityEngine;
using UnityEngine.UI;
public class FlameManager_1 : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public int flameCount = 0;
    public Text flameText;
    public GameObject Door;
    public bool isDoorOpen = false;
    public GameObject winPanel;
    public int targetFlames = 9;
    void Start()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        flameText.text = " : " + flameCount.ToString();
        if (flameCount >= 4 && !isDoorOpen)
        {
            isDoorOpen = true;
            if(Door != null)
            {
                Destroy(Door);
            }
        }
        if (flameCount >= targetFlames)
        {
            winPanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }
}
