import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { MenuItem, MenuService } from '../../core/services/menu.service';

@Component({
  selector: 'app-navigation',
  templateUrl: './navigation.component.html',
  styleUrls: ['./navigation.component.scss']
})
export class NavigationComponent implements OnInit {
  menu$: Observable<MenuItem[]>;

  constructor(private menuService: MenuService) { }

  ngOnInit(): void {
    this.menu$ = this.menuService.getMenu();
  }
}
