using UnityEngine;

public class PopupSelectable : TLabOutlineSelectable
{
    // TLabOutlineSelectableを継承したPopupTextManagerも管理できるクラスを作ってる途中

    public PopupTextManager PopupManager
    {
        get
        {
            return m_popupManager;
        }

        set
        {
            m_popupManager = value;
        }
    }

    /// <summary>
    /// PopupManagerから自身に関連するTextControllerを参照するときは，m_indexを使用する
    /// m_popupManager.GetTextController(m_index);
    /// </summary>
    public int Index
    {
        set
        {
            m_index = value;
        }
    }

    [SerializeField] private PopupTextManager m_popupManager;
    [SerializeField] private int m_index;

    protected override void Start()
    {
        base.Start();

        // ここに処理を追加


        // やりたいこと:
        // ポインターをかざしたらCanvasをフェードインし，外したらフェードアウトする処理を作りたい
        // フェードイン   : Canvasが少しずつ大きくなる (TextController.FadeIn()  <--- 試作はした)
        // フェードアウト : Canvasが少しずつ小さくなる (TextController.FadeOut() <--- 試作はした)

        // 注意.
        // TextController.FadeIn(), TextController.FadeOut()が正しく動くか分からない(検証してない)
        // なんかおかしかったら直してほしい

        // PopupManagerからTextControllerを取得する例:
        // m_popupManager.GetTextController(m_index);

        // レーザーポインターの操作に関するコールバック実装:

        // ex.0
        // bool prevFrame = m_selected
        // if(m_selected && !prevFrame)
        // {
        //      // レーザーポインターをかざした
        //      // TextController.FadeIn()
        // }

        // ex.1
        // bool prevFrame = m_selected
        // if(!m_selected && prevFrame)
        // {
        //      // レーザーポインターを外した
        //      // TextController.FadeOut()
        // }
    }

    protected override void Update()
    {
        base.Update();
    }

    protected virtual void OnDestroy()
    {
        Destroy(m_popupManager);
    }
}
