import {
  Directive,
  Input,
  OnDestroy,
  OnInit,
  TemplateRef,
  ViewContainerRef,
} from '@angular/core';
import { Subscription } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';

@Directive({
  selector: '[appIfRole]',
})
export class IfRoleDirective implements OnInit, OnDestroy {
  @Input('appIfRole') roles!: string[];
  private subscription!: Subscription;

  constructor(
    private templateRef: TemplateRef<any>,
    private viewContainer: ViewContainerRef,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.subscription = this.authService.user$.subscribe((user) => {
      this.viewContainer.clear();
      if (user) {
        const userRoleText = this.authService.getUserRole();
        if (userRoleText && this.roles.includes(userRoleText)) {
          this.viewContainer.createEmbeddedView(this.templateRef);
        }
      }
    });
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }
}
