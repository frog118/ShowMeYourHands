using Verse;

namespace FacialStuff;

public class BodyProps
{
    public BodyPartRecord _rightFoot;
    public BodyPartRecord _leftFoot;
    public BodyPartRecord _rightHand;
    public BodyPartRecord _leftHand;


    public BodyPartRecord _rightEye;
    public BodyPartRecord _leftEye;
    public BodyPartRecord _rightEar;
    public BodyPartRecord _leftEar;
    public CompBodyAnimator _anim;
    public Hediff _hediff;

    public BodyProps(Hediff hediff, CompBodyAnimator anim, BodyPartRecord leftEye, BodyPartRecord rightEye,
        BodyPartRecord leftHand, BodyPartRecord rightHand, BodyPartRecord leftFoot, BodyPartRecord rightFoot)
    {
        this._hediff = hediff;
        this._anim = anim;
        this._leftEye = leftEye;
        this._rightEye = rightEye;
        this._leftHand = leftHand;
        this._rightHand = rightHand;
        this._leftFoot = leftFoot;
        this._rightFoot = rightFoot;
    }
}