#import <UIKit/UIKit.h>

extern "C" void SymbiosisHapticImpact(int style)
{
    if (@available(iOS 10.0, *))
    {
        UIImpactFeedbackStyle feedbackStyle = style <= 0
            ? UIImpactFeedbackStyleLight
            : UIImpactFeedbackStyleMedium;
        UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:feedbackStyle];
        [generator prepare];
        [generator impactOccurred];
    }
}
