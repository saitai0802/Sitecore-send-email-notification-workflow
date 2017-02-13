# Sitecore - Sending email notification workflow
Customizing Sitecore's Workflow Email Action (Sending to users who has premission in next workflow state)

Since users often want to customize their workflow notifications to be more meaningful, and website adminstrator wannt to have a easier setting to modify the email setting. So I wrote this Sitecore plugin for that.
Let's see what functionalities we have.

1. Sending email to users who have any premissions on the next Workflow state.
2. Optional choice to decide whether Emails need to send to a content author.
3. Dynamic Email template - Using custom placeholder format like [ItemName]

###Configuration
* Find an existing Email Action in your workflow or navigate to the Email Action's standard values at /sitecore/templates/System/Workflow/Email Action.
* Change the Type field to point to your class and assembly.
* Use your new tokens in the To, From, Title and Message field on your Email Action item.
* Screenshot of Email Action item configured

Screen captures for reference
---
**Email workflow entity**:<br/>
![Image of Workflow](https://github.com/saitai0802/Sitecore-send-email-notification-workflow/blob/master/images/workflow.jpg)

**Email template entity**:<br/>
![Image of Email Template](https://github.com/saitai0802/Sitecore-send-email-notification-workflow/blob/master/images/email-template.jpg)
