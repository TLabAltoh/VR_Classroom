using UnityEngine;

public class PopupSelectable : TLabOutlineSelectable
{
    // TLabOutlineSelectable���p������PopupTextManager���Ǘ��ł���N���X������Ă�r��

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
    /// PopupManager���玩�g�Ɋ֘A����TextController���Q�Ƃ���Ƃ��́Cm_index���g�p����
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

        // �����ɏ�����ǉ�


        // ��肽������:
        // �|�C���^�[������������Canvas���t�F�[�h�C�����C�O������t�F�[�h�A�E�g���鏈������肽��
        // �t�F�[�h�C��   : Canvas���������傫���Ȃ� (TextController.FadeIn()  <--- ����͂���)
        // �t�F�[�h�A�E�g : Canvas���������������Ȃ� (TextController.FadeOut() <--- ����͂���)

        // ����.
        // TextController.FadeIn(), TextController.FadeOut()��������������������Ȃ�(���؂��ĂȂ�)
        // �Ȃ񂩂������������璼���Ăق���

        // PopupManager����TextController���擾�����:
        // m_popupManager.GetTextController(m_index);

        // m_index: ���g�ƕR�Â���TextController�̃C���f�b�N�X(PopupTextManager�Ŋ���U���Ă���)

        // ���[�U�[�|�C���^�[�̑���Ɋւ���R�[���o�b�N����:

        // m_selected       : ����̃t���[���Ń��[�U�[�|�C���^�[�͓������Ă�����
        // m_prevSelected   : �O��̃t���[���Ń��[�U�[�|�C���^�[�͓������Ă�����

        // ex.0
        // if(m_selected && !m_prevSelected)
        // {
        //      // ���[�U�[�|�C���^�[����������
        //      // TextController.FadeIn()
        // }

        // ex.1
        // if(!m_selected && m_prevSelected)
        // {
        //      // ���[�U�[�|�C���^�[���O����
        //      // TextController.FadeOut()
        // }
    }

    protected override void Update()
    {
        base.Update();
    }
}
