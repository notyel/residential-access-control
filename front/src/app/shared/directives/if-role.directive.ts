import { Directive, Input, OnDestroy, OnInit, TemplateRef, ViewContainerRef } from '@angular/core';
import { Subscription } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';

@Directive({
  selector: '[appIfRole]'
})
export class IfRoleDirective implements OnInit, OnDestroy {
  @Input('appIfRole') roles: string[];
  private subscription: Subscription;

  constructor(
    private templateRef: TemplateRef<any>,
    private viewContainer: ViewContainerRef,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    this.subscription = this.authService.userRole$.subscribe(userRole => {
      this.viewContainer.clear();
      if (this.roles.includes(userRole)) {
        this.viewContainer.createEmbeddedView(this.templateRef);
      }
    });
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }
}
