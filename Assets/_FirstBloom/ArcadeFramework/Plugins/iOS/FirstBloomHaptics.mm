#import <UIKit/UIKit.h>

extern "C" void FirstBloom_PlayHaptic(int hapticType)
{
    if (@available(iOS 10.0, *))
    {
        dispatch_async(dispatch_get_main_queue(), ^{
            if (hapticType == 3 || hapticType == 4)
            {
                UINotificationFeedbackType notificationType =
                    hapticType == 3 ? UINotificationFeedbackTypeSuccess : UINotificationFeedbackTypeError;
                UINotificationFeedbackGenerator *generator = [[UINotificationFeedbackGenerator alloc] init];
                [generator prepare];
                [generator notificationOccurred:notificationType];
                return;
            }

            UIImpactFeedbackStyle style = UIImpactFeedbackStyleLight;
            if (hapticType == 1)
            {
                style = UIImpactFeedbackStyleMedium;
            }
            else if (hapticType == 2)
            {
                style = UIImpactFeedbackStyleHeavy;
            }

            UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:style];
            [generator prepare];
            [generator impactOccurred];
        });
    }
}
